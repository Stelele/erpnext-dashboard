# Frontend IndexedDB Cache Layer — Design Spec

**Date**: 2026-06-14
**Status**: Approved

## Problem

In production, backend API latency is severe (up to 60 seconds per request). The backend serves reference/configuration data (users, companies, sites, settings, expense types, expense mappings, chart colors) that rarely or never changes. The backend already has server-side in-memory caching via MediatR pipeline but no HTTP caching headers, so every request downloads full response bodies. Small wins (network optimization, etc.) have been tried and had negligible impact.

## Goal

Eliminate backend API latency from the user experience by serving all backend API responses from a local IndexedDB cache. Only ERPNext API calls remain network-bound (they are already fast).

## Architecture

### Component Diagram

```
┌──────────────────────────────────────────────────────────────┐
│  Main Thread (Vue App)                                       │
│                                                              │
│  Stores → CachedApiClient                                    │
│              │                                               │
│              ├─ cache hit  → Dexie DB → return immediately   │
│              ├─ cache miss → ApiSingleton (fetch) → Dexie DB │
│              └─ mutation   → ApiSingleton → Dexie DB purge   │
│                                                              │
│  App.vue → CachedApiClient.bootstrap()                       │
│              │                                               │
│              └─ meta version check → empty/mismatch: full    │
│                 sync all GET endpoints, show loading bar     │
└──────────────────────────────────────────────────────────────┘
                         │ postMessage(SET_TOKEN, SYNC_ALL)
                         ▼
┌──────────────────────────────────────────────────────────────┐
│  Web Worker (cacheSyncWorker.ts)                             │
│                                                              │
│  Stores Auth0 token (received via message)                   │
│  On message SYNC_ALL: fetch all GET endpoints → Dexie        │
│  setInterval 5min: fetch all GET endpoints → Dexie           │
│  Reports progress via postMessage back to main thread        │
└──────────────────────────────────────────────────────────────┘
```

### Dexie Database Schema

```typescript
interface DashboardCacheDB extends Dexie {
  users: Table<UserResponse, string>;              // key: userId
  companies: Table<CompanyResponse, string>;       // key: companyId
  sites: Table<SiteResponse, string>;              // key: siteId
  expenseTypes: Table<ExpenseTypeResponse, string>; // key: expenseTypeId
  expenseMappings: Table<CompanyExpenseMappingResponse, string>;  // key: companyId_expenseTypeId
  companySettings: Table<CompanySettingsResponse, string>;  // key: companyId
  chartColors: Table<ChartColorEntry, string>;    // key: primaryColor
  logos: Table<LogoEntry, string>;                 // key: siteId
  meta: Table<CacheMetaEntry, string>;             // key: "singleton"
}
```

- **Key format**: natural string keys (not GUIDs) — `userId` for users, `companyId` for companies, etc.
- **Expense mappings** composite key: `${companyId}_${expenseTypeId}`
- **Meta table**: single row tracking `dbVersion` (incremented on schema changes) and `lastFullSync` timestamp

### CachedApiClient

Wraps the existing `ApiSingleton` (openapi-fetch typed client) with Dexie read-through cache logic:

- `GET` → check Dexie table → hit: return cached; miss: fetch from ApiSingleton, store in Dexie, return
- `POST/PUT/DELETE` → forward to ApiSingleton, then clear corresponding table (invalidation)
- `bootstrap()` → checks `meta.dbVersion` against current schema version → mismatch/empty triggers full sync
- Implements the same `openapi-fetch` `Client<paths>` interface so stores can swap with minimal changes
- Progress events from worker forwarded via Vue composable for UI consumption

### Web Worker (cacheSyncWorker.ts)

- Receives `postMessage` with `{ type: "SET_TOKEN", token: string }` to maintain auth
- Receives `{ type: "SYNC_ALL" }` to perform a full refresh of all GET endpoints
- Runs `setInterval` at 5-minute jittered interval for periodic full refresh
- Has its own `openapi-fetch` client instance (works in workers — just fetch under the hood)
- Posts progress events back to main thread: `{ type: "PROGRESS", current: number, total: number }` and `{ type: "COMPLETE" }`
- Posts errors back for logging but never blocks the UI

### Bootstrap Flow

```
App.vue onBeforeMount
  → AuthStore.login() (Auth0 authentication)
  → CachedApiClient.bootstrap()
      → Read Dexie meta table
      → meta.dbVersion === CURRENT_SCHEMA_VERSION → skip, cache is valid
      → mismatch or empty → full sync:
          → Show loading indicator (reuse LoadingBar.vue)
          → Fetch all backend GET endpoints:
              /users (list + current user)
              /companies (list)
              /sites (list)
              /api/expense-types
              /api/companies/{companyId}/expense-mappings
              /api/companies/{companyId}/settings
              /api/theme/chart-colors
          → Bulk-put into Dexie
          → Set meta.dbVersion = CURRENT_SCHEMA_VERSION, meta.lastFullSync = now
          → Hide loading indicator
  → Proceed to normal DataStore.getData() (ERPNext calls)
```

### Files

| Action | File | Purpose |
|--------|------|---------|
| **New** | `frontend/src/services/db/index.ts` | Dexie schema definition + DB singleton |
| **New** | `frontend/src/services/cache/CachedApiClient.ts` | Read-through cache wrapper |
| **New** | `frontend/src/services/cache/cacheSyncWorker.ts` | Web worker for background refresh |
| **New** | `frontend/src/composables/useCacheSync.ts` | Composable exposing worker state to UI |
| **Modify** | `frontend/src/services/api/index.ts` | Export ApiSingleton class for use by worker |
| **Modify** | `frontend/src/stores/DataStore.ts` | Replace `ApiSingleton` with `CachedApiClient` |
| **Modify** | `frontend/src/stores/AuthStore.ts` | Replace `ApiSingleton` with `CachedApiClient` |
| **Modify** | `frontend/src/composables/useChartColors.ts` | Replace `idb-keyval` with Dexie `chartColors` table |
| **Modify** | `frontend/src/composables/useCompanyTheme.ts` | Route through CachedApiClient |
| **Modify** | `frontend/src/App.vue` | Add bootstrap call before data fetch |
| **Remove** | `idb-keyval` from `package.json` | No longer needed |

### Error Handling

- Worker sync failure: silent retry next cycle; UI continues with last-known-good Dexie data
- Network offline: Dexie serves stale data; no error surfaced
- Empty cache + network down: show error state (only path where user sees a problem)
- Auth0 token expired in worker: next `SET_TOKEN` message resolves it; worker retries

### Edge Cases

- **Schema version bump**: bump `CURRENT_SCHEMA_VERSION` constant → next `bootstrap()` detects mismatch → wipes all tables except meta → full sync
- **Company switch**: existing `AuthStore` flow already re-fetches; cached data for new company likely already in Dexie from periodic sync
- **Concurrent writes**: Dexie is single-threaded per origin; no race conditions within the worker
- **Worker creation failure**: fall back to main-thread sync (degraded but functional)

### Dependencies

- **Add**: `dexie` (^4.x) — already well-established IndexedDB wrapper
- **Remove**: `idb-keyval` — consolidated into Dexie
- **Existing**: `openapi-fetch` stays, `vite-plugin-pwa` stays (service worker is orthogonal)

### Non-Goals

- ERPNext API caching (those APIs are fast, leave as-is)
- Offline-first architecture (a natural side effect, but not the primary goal)
- Differential/since-based polling (payloads are tiny, full sync is simpler and sufficient)
- Backend HTTP cache header changes (not needed if frontend bypasses network entirely)
