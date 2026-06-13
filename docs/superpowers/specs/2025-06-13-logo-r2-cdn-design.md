# Design: Logo R2 CDN Optimization

**Date:** 2025-06-13

## Problem

The current logo endpoint (`GET /sites/{siteId}/logo?company={name}`) proxies raw image bytes from ERPNext on every request. In production, this is too slow — each request makes 2 HTTP calls to ERPNext (fetch company JSON, then fetch image file), and no caching exists.

## Solution

Serve company logos from a public Cloudflare R2 bucket (`public-apps-bucket`) with a CDN URL. A background sync service periodically uploads logos to R2. The logo endpoint switches from returning raw bytes to returning a CDN URL.

## Architecture

### New Component: `LogoR2SyncService`

A hosted background service that syncs company logos from ERPNext to R2.

- **Startup**: Runs immediately on application start
- **Schedule**: Repeats on a configurable interval (`LogoSyncIntervalMinutes`, default 1440 = daily)
- **Flow per sync cycle**:
  1. Query all `Site` entities from the database
  2. For each site, call `GET {site.Url}/api/resource/Company` with `limit_page_length=100` to get all companies
  3. For each company, get the `company_logo` file path
  4. Download the logo image from `{site.Url}{logoPath}` with the site's API token
  5. Upload the image bytes to R2 at key: `dashboard/sites/{siteId}/logo_{companyName}.png`
  6. Log results (success count, failure count)

### Modified Component: Logo Endpoint (`SitesEndpoints.cs`)

**Before:** Fetches company from ERPNext, fetches image bytes, returns raw bytes.

**After:**
- Construct R2 object key: `dashboard/sites/{siteId}/logo_{companyName}.png`
- Check if object exists in R2 bucket (`ObjectExistsAsync`)
- If exists: return JSON `{ "url": "{CDNBaseUrl}/dashboard/sites/{siteId}/logo_{companyName}.png" }`
- If not exists: return 404 Not Found (frontend shows `/logo.png` fallback)

### Modified Component: R2 Configuration

Add to existing `R2Options` class and `appsettings.json`:
- `PublicBucketName` — the public bucket name (e.g., `"public-apps-bucket"`)
- `CDNBaseUrl` — base URL for constructing CDN URLs (e.g., `"https://pub-xxx.r2.dev"`)
- `LogoSyncIntervalMinutes` — sync interval for logos (default 1440)

A second `AmazonS3Client` instance is needed for the public bucket (different bucket URL). The `IR2StorageService` interface gains a `PublicBucket` property or a separate method/constructor parameter to target the public bucket.

### Modified Component: Frontend

**`AuthStore.ts`:**
- `loadCurrentLogo()` fetches the endpoint, parses JSON response `{ url }`, sets `logo.value = url`
- No longer treats the endpoint response as a direct image src
- Fallback to `/logo.png` on fetch failure (including 404)

**`CompanySwitcherModalContent.vue`:**
- Same adaptation — fetches JSON, extracts `url`, uses as image src

**`services/api/logo.ts`:**
- `getLogoProxyUrl()` — update function name/semantics to `getLogoUrl()` (still calls same endpoint, but now returns JSON)

### No Changes
- `DashboardLayout.vue` — still binds `authStore.logo` to `<img :src>`, unchanged
- `App.vue` favicon — unchanged, still uses `authStore.logo`
- R2 backup credentials (`AccountId`, `AccessKeyId`, `SecretAccessKey`) — reused, unchanged

## Data Flow

```
Startup / Interval tick
  → LogoR2SyncService iterates sites
    → GET {erpnext}/api/resource/Company (all companies)
    → GET {erpnext}/files/{logo_path} (image bytes)
    → Upload to R2: dashboard/sites/{siteId}/logo_{company}.png

Frontend requests logo
  → GET /sites/{siteId}/logo?company=X
    → Construct R2 key, check object exists
    → Return { url: "https://<cdn>/dashboard/sites/{siteId}/logo_X.png" }
  → <img src="https://<cdn>/dashboard/sites/{siteId}/logo_X.png" />
    → Hits R2 directly, zero backend overhead
```

## R2 Key Structure

```
dashboard/sites/{siteId}/logo_{companyName}.png
```

All files under the `dashboard/` prefix in the public bucket. The bucket must have public access enabled for the `dashboard/` prefix (or the entire bucket) for CDN URLs to work.

## Configuration

```json
"R2": {
    "AccountId": "<secret>",
    "AccessKeyId": "<secret>",
    "SecretAccessKey": "<secret>",
    "BackupBucketName": "apps-bucket",
    "PublicBucketName": "public-apps-bucket",
    "CDNBaseUrl": "https://pub-xxx.r2.dev",
    "SyncIntervalMinutes": 15,
    "LogoSyncIntervalMinutes": 1440,
    "RestoreMaxRetries": 3
}
```

Environment variables (for deployment):
- `R2__AccountId`
- `R2__AccessKeyId`
- `R2__SecretAccessKey`
- `R2__BackupBucketName`
- `R2__PublicBucketName`
- `R2__CDNBaseUrl`
- `R2__SyncIntervalMinutes`
- `R2__LogoSyncIntervalMinutes`
- `R2__RestoreMaxRetries`

## Error Handling
- Sync failures for individual sites/companies are logged and skipped — don't block the sync cycle
- If R2 object doesn't exist, endpoint returns 404 — frontend shows fallback `/logo.png`
- If R2 is unreachable during endpoint check, return 503 with fallback behavior
- If ERPNext is unreachable during sync, log error and skip that site

## Testing

- Unit test: `LogoR2SyncService` sync logic with mocked HTTP and R2
- Integration test: Logo endpoint returns correct JSON URL when object exists
- Integration test: Logo endpoint returns 404 when object doesn't exist
- Verify frontend AuthStore handles JSON response and fallback correctly
