# Security Hardening: User-Aware Data Access + Caching Fix

> **Goal:** Fix the cross-user data leak in the caching layer and enforce server-side user-company data isolation. Address additional security issues discovered during audit.

**Tech Stack:** .NET 10, MediatR, IMemoryCache, EF Core, Auth0 JWT, Vue 3, Dexie/IndexedDB

---

## Problem Summary

The backend caching pipeline (`CachePipelineBehavior`) caches query results without the requesting user's identity in the cache key. Combined with the fact that **no query handler filters results by the current user**, this means any authenticated user can see all companies, all sites, and all user data — and the cache persists this across users.

The frontend filters companies client-side in `AuthStore.ts`, but this is trivially bypassed.

Additional issues found: CORS misconfiguration, missing security headers, Auth0 management token logged in plain text, CreateUser endpoint using wrong permission scope.

---

## Architecture

### New Files

```
backend/Application/Users/
├── IUserContext.cs              — Interface: UserId, CompanyIds, HasCompany()
├── UserContext.cs               — Mutable implementation (populated by middleware)

backend/Host/Middleware/
├── UserContextMiddleware.cs     — Extracts userId from JWT → DB lookup → populates UserContext
├── SecurityHeadersMiddleware.cs — Sets X-Content-Type-Options, X-Frame-Options, etc.
```

### Modified Files

```
backend/Application/Caching/
├── CachePipelineBehavior.cs     — Append :{userId} to all cache keys

backend/Application/Companies/
├── GetCompaniesQueryHandler.cs  — Filter by userContext.CompanyIds
├── GetCompanyByIdQueryHandler.cs — Reject if company not in user's list

backend/Application/Sites/
├── GetSitesQueryHandler.cs      — Filter by user's companies → sites
├── GetSiteByIdQueryHandler.cs   — Reject if site not linked to user's companies

backend/Application/Users/
├── GetUsersQueryHandler.cs      — Return only the calling user
├── GetUserByIdQueryHandler.cs   — Reject if ID != caller's ID

backend/Application/Users/
├── AddUserToCompanyCommand.cs   — Add [InvalidateCache] for companies category

backend/Host/Program.cs (or Endpoints/DependancyInjection.cs)
├── CORS: Move before auth, restrict origins
├── Register IUserContext, middleware

backend/Infrastructure/Auth0/Auth0UserProvisioner.cs
├── Remove token log line

backend/Endpoints/Endpoints/UsersEndpoints.cs
├── Fix POST /users permission to CreateUsers

frontend/src/stores/AuthStore.ts
├── Remove client-side company filtering

frontend/src/services/cache/CachedApiClient.ts
├── Wipe IndexedDB on user change / logout
```

---

## Section 1: IUserContext Service

### IUserContext Interface

```csharp
public interface IUserContext
{
    Guid UserId { get; }
    IReadOnlyList<Guid> CompanyIds { get; }
    bool HasCompany(Guid companyId);
}
```

### UserContext Implementation

```csharp
public class UserContext : IUserContext
{
    public Guid UserId { get; set; }
    public IReadOnlyList<Guid> CompanyIds { get; set; } = Array.Empty<Guid>();
    public bool HasCompany(Guid companyId) => CompanyIds.Contains(companyId);
}
```

Registered as **scoped** — one instance per HTTP request. The middleware sets the properties.

### User Context Caching (Optimization)

The middleware hits DB on every request. To avoid this, a small in-memory cache maps `Auth0UserId → local User ID + Company IDs` with a short TTL (1 minute). When a user-company link changes, the `[InvalidateCache]` attribute on `AddUserToCompanyCommand`/`RemoveUserFromCompanyCommand` will evict this cache too (or it naturally expires within 1 minute).

This cache is separate from the query/command MediatR cache — it lives in the middleware itself or a dedicated `UserContextCache` singleton.

### Test Compatibility

Existing tests bypass `UserContextMiddleware`. `IUserContext` will be registered as scoped with default values (`Guid.Empty`, empty array). Tests that need a specific user can resolve `UserContext` and set properties directly.

---

## Section 2: UserContextMiddleware

### Pipeline position

```
app.UseCors("AllowFrontend");        // MOVED: before auth
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserContextMiddleware>();  // NEW: after auth, before endpoints
app.MapEndpoints();
app.UseMiddleware<SecurityHeadersMiddleware>();  // NEW
```

### Logic

1. Check `context.User.Identity?.IsAuthenticated` — skip if not
2. Read `https://meta.dashboard.com/user_id` claim from `ClaimsPrincipal`
3. If missing → 401
4. Parse as `Guid` → look up in DB (or in-memory cache) for the `User` record by `Auth0UserId` field, including `.Companies`
5. If not found → 401 (user exists in Auth0 but not yet provisioned in local DB)
6. Populate `UserContext` → resolve from `context.RequestServices` scoped container
7. Set `UserContext.UserId` and `UserContext.CompanyIds`
8. Call `await next()`

---

## Section 3: Handler Changes — User-Aware Filtering

Every handler that returns multi-tenant data injects `IUserContext` and adds a `.Where()` clause.

### GetCompaniesQueryHandler

```csharp
var query = request.CompanyIds?.Length > 0
    ? db.Companies.Where(c => userContext.CompanyIds.Contains(c.Id)
                           && request.CompanyIds.Contains(c.Id))
    : db.Companies.Where(c => userContext.CompanyIds.Contains(c.Id));
```

### GetCompanyByIdQueryHandler

If company exists but user isn't a member → return `null` (controller returns 404, no info leak).

```csharp
var company = await db.Companies
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == request.Id && userContext.CompanyIds.Contains(c.Id));
return company == null ? null : CompanyResponse.FromDomain(company);
```

### GetSitesQueryHandler

Filter sites to only those linked to the user's companies:

```csharp
var query = request.Sites?.Length > 0
    ? db.Sites.Where(s => s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id))
                       && request.Sites.Contains(s.Id))
    : db.Sites.Where(s => s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id)));
```

### GetSiteByIdQueryHandler

Return null if site isn't linked to user's companies:

```csharp
var site = await db.Sites
    .Include(s => s.Companies)
    .FirstOrDefaultAsync(s => s.Id == request.Id
        && s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id)));
```

### GetUsersQueryHandler

Return only the calling user:

```csharp
var users = await db.Users
    .Include(u => u.Companies)
    .Where(u => u.Id == userContext.UserId)
    .ToListAsync(cancellationToken);
```

### GetUserByIdQueryHandler

Reject if requested ID doesn't match caller:

```csharp
if (request.Id != userContext.UserId)
    return null;
```

### Company-specific Endpoints

`GET /companies/{companyId}/settings`, `GET/PUT /companies/{companyId}/expense-mappings`, `PUT /companies/{companyId}/settings` — all check `userContext.HasCompany(companyId)` and return 403/404 if unauthorized.

### Unaffected Handlers

- `GetChartColorsQueryHandler` — global data, no user filtering needed
- `GetCompanyLogoQueryHandler` — already doesn't use `[Cache]`, remains public

---

## Section 4: Cache Key Changes

### New key format

Cache keys now include the user ID:

```
Before:                    After:
companies:guid1,guid2   → companies:{userId}:guid1,guid2
company:guid             → company:{userId}:guid
sites:                   → sites:{userId}:
settings:guid            → settings:{userId}:guid
expense_types:           → expense_types:{userId}:
expense_mappings:guid    → expense_mappings:{userId}:guid
users:                   → users:{userId}:
```

### Implementation in CachePipelineBehavior.BuildKey

```csharp
private static string BuildKey<TRequest>(TRequest request, Guid userId)
{
    var prefix = GetCachePrefix(request);
    var props = typeof(TRequest).GetProperties()
        .OrderBy(p => p.Name)
        .Select(p => FormatValue(p.GetValue(request)));
    return $"{prefix}:{userId}:" + string.Join(":", props);
}
```

The userId comes from a new method signature that accepts `IUserContext` (or the pipeline resolves it from DI itself). For requests without `[Cache]`, the key is never built.

### Cache Invalidation

Category-based invalidation still works: when a company is created, `[InvalidateCache(Category = "companies")]` cancels the token → evicts ALL entries with that prefix, including all users' `companies:{*}` keys. This is coarse but correct.

### New Invalidation: User-Company Link Changes

`AddUserToCompanyCommand` and `RemoveUserFromCompanyCommand` need `[InvalidateCache(Category = "companies")]` to clear the affected user's company list cache.

---

## Section 5: ApiToken in SiteResponse

`ApiToken` stays in `SiteResponse`. Since handlers now filter sites by user's company membership, a user only sees tokens for sites their companies belong to. No new endpoint needed.

---

## Section 6: CORS, Security Headers, Auth0 Token Fix

### CORS

- **Move**: `app.UseCors("AllowFrontend")` before `app.UseAuthentication()`
- **Restrict**: Replace `AllowAnyOrigin()` with the frontend origin from config. Change to:
  ```csharp
  policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
  ```

### Security Headers

New `SecurityHeadersMiddleware` sets:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Strict-Transport-Security: max-age=31536000` (already using HTTPS redirect)

### Auth0 Token Log Removal

Remove `logger.LogInformation($"Obtained Auth0 management API token: {token.AccessToken}")` from `Auth0UserProvisioner.cs:70`.

### CreateUser Permission Fix

Change `POST /users` from `.RequireAuthorization(Permissions.UpdateUsers)` to `.RequireAuthorization(Permissions.CreateUsers)`.

---

## Section 7: Frontend Changes

### AuthStore — Remove Client-Side Filtering

In `AuthStore.ts`, remove the company filtering logic. After the backend enforces filtering, the API returns only the user's companies. The `update()` function changes to:

```typescript
const data = await client.getUser(userId.value);
user.value = data;
companies.value = await client.getUserCompanies(); // Already filtered by backend
```

### CachedApiClient — Wipe on User Change

On logout or when the Auth0 user ID changes, delete the IndexedDB database entirely. Add a check in `bootstrap()`:

```typescript
const currentUserId = getUserIdFromToken();
const storedUserId = await db.meta.get('userId');
if (storedUserId !== currentUserId) {
    await db.delete();
    await db.open();
    // Re-bootstrap
}
```

Store `userId` in the `meta` table alongside `dbVersion` and `lastFullSync`.

### cacheSyncWorker — No Changes

The periodic sync fetches from the same endpoints which now return only the user's data. No changes needed.

---

## Error Handling

- **User not found in local DB**: 401 Unauthorized (user exists in Auth0 but not provisioned)
- **Requested company not in user's list**: 404 Not Found (no info leak — can't distinguish "doesn't exist" from "not authorized")
- **Requesting another user's profile**: 404 Not Found
- **Requesting site not linked to user's companies**: 404 Not Found

---

## Backward Compatibility

- No API schema changes (ApiToken stays, same response shapes)
- Frontend IndexedDB schema version bump to force re-sync on next load
- All existing tests must pass after changes; new tests for authorization logic
