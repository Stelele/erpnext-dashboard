# Security Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix cross-user data leak in caching layer, enforce server-side user-company data isolation, and address additional security issues (CORS, security headers, Auth0 token log, CreateUser permission).

**Architecture:** New `IUserContext` scoped service + `UserContextMiddleware` that extracts userId from JWT claims and resolves user's company memberships. All query handlers inject `IUserContext` and filter results. Cache keys include userId. CORS moved before auth and restricted. Frontend removes client-side filtering; IndexedDB wiped on user change.

**Tech Stack:** .NET 10, MediatR, IMemoryCache, EF Core, Auth0 JWT, Vue 3, Dexie/IndexedDB

---

## File Structure

```
New files:
  backend/Application/Users/IUserContext.cs
  backend/Application/Users/UserContext.cs
  backend/Host/Middleware/UserContextMiddleware.cs
  backend/Host/Middleware/SecurityHeadersMiddleware.cs

Modified files (backend):
  backend/Application/DependancyInjection.cs          — Register IUserContext, UserContextMiddleware
  backend/Host/Program.cs                             — Add middleware to pipeline
  backend/Endpoints/DependancyInjection.cs             — Fix CORS (move + restrict), add UserContextMiddleware
  backend/Application/Caching/CachePipelineBehavior.cs — Include userId in cache keys
  backend/Application/Companies/GetCompaniesQueryHandler.cs
  backend/Application/Companies/GetCompanyByIdQueryHandler.cs
  backend/Application/Sites/GetSitesQueryHandler.cs
  backend/Application/Sites/GetSiteByIdQueryHandler.cs
  backend/Application/Users/GetUsersQueryHandler.cs
  backend/Application/Users/GetUserByIdQueryHandler.cs
  backend/Application/CompanySettings/GetCompanySettingsQuery.cs
  backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs
  backend/Application/CompanyExpenseMappings/GetCompanyExpenseMappingsQuery.cs
  backend/Application/CompanyExpenseMappings/UpsertCompanyExpenseMappingsCommand.cs
  backend/Application/Users/AddUserToCompanyCommand.cs
  backend/Application/Users/RemoveUserFromCompanyCommand.cs
  backend/Infrastructure/Auth0/Auth0UserProvisioner.cs — Remove token log
  backend/Endpoints/Endpoints/UsersEndpoints.cs       — Fix CreateUser permission
  backend/Application/Users/CreateUserCommandHandler.cs — Add Auth0 provisioning (only if missing)
  backend/Tests/TestAuthHandler.cs                     — Add custom namespace claim
  backend/Tests/IntegrationTestFactory.cs              — Register IUserContext for tests
  backend/Tests/CachingTests.cs                        — Update cache key assertions

Modified files (frontend):
  frontend/src/stores/AuthStore.ts                     — Remove client-side filtering
  frontend/src/services/cache/CachedApiClient.ts       — Wipe IndexedDB on user change
  frontend/src/services/db/index.ts                    — Bump schema version
```

---

### Task 1: Create IUserContext Interface and UserContext Implementation

**Files:**
- Create: `backend/Application/Users/IUserContext.cs`
- Create: `backend/Application/Users/UserContext.cs`

- [ ] **Step 1: Write IUserContext.cs**

```csharp
namespace Application.Users;

public interface IUserContext
{
    Guid UserId { get; }
    IReadOnlyList<Guid> CompanyIds { get; }
    bool HasCompany(Guid companyId);
}
```

- [ ] **Step 2: Write UserContext.cs**

```csharp
namespace Application.Users;

public class UserContext : IUserContext
{
    public Guid UserId { get; set; }
    public IReadOnlyList<Guid> CompanyIds { get; set; } = Array.Empty<Guid>();
    public bool HasCompany(Guid companyId) => CompanyIds.Contains(companyId);
}
```

- [ ] **Step 3: Register IUserContext in Application/DependancyInjection.cs**

In `backend/Application/DependancyInjection.cs`, add after `builder.Services.AddSingleton<CategoryCacheTokenStore>();`:

```csharp
builder.Services.AddScoped<UserContext>();
builder.Services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserContext>());
```

- [ ] **Step 4: Commit**

---

### Task 2: Create UserContextMiddleware

**Files:**
- Create: `backend/Host/Middleware/UserContextMiddleware.cs`

- [ ] **Step 1: Write UserContextMiddleware.cs**

```csharp
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Host.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserContext userContext, DashboardDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("https://meta.dashboard.com/user_id");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await db.Users
                    .AsNoTracking()
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    userContext.UserId = user.Id;
                    userContext.CompanyIds = user.Companies.Select(c => c.Id).ToList();
                }
            }
        }

        await _next(context);
    }
}
```

- [ ] **Step 2: Register middleware in Endpoints/DependancyInjection.cs MapApi method**

In `backend/Endpoints/DependancyInjection.cs`, change the `MapApi` method. Move `app.UseCors("AllowFrontend")` before `app.UseAuthentication()` and add UserContextMiddleware after `app.UseAuthorization()`:

```csharp
public static WebApplication MapApi(this WebApplication app)
{
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<UserContextMiddleware>();

    app
        .MapCompanyEndpoints()
        .MapSitesEndpoints()
        .MapUsersEndpoints()
        .MapExpenseEndpoints()
        .MapThemeEndpoints();

    return app;
}
```

- [ ] **Step 3: Add required using in Endpoints/DependancyInjection.cs**

Add at top:
```csharp
using Host.Middleware;
```

- [ ] **Step 4: Commit**

---

### Task 3: Modify CachePipelineBehavior to Include userId in Keys

**Files:**
- Modify: `backend/Application/Caching/CachePipelineBehavior.cs`

- [ ] **Step 1: Update CachePipelineBehavior to inject IUserContext and include userId in keys**

```csharp
using Application.Users;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Application.Caching;

public class CachePipelineBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    CategoryCacheTokenStore tokenStore,
    IUserContext userContext
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (typeof(TRequest).GetCustomAttributes(typeof(CacheAttribute), false).FirstOrDefault() is CacheAttribute cacheAttr)
        {
            var key = BuildKey(cacheAttr.KeyPrefix, request, userContext.UserId);
            if (cache.TryGetValue<TResponse>(key, out var cached) && cached is not null)
                return cached;

            var result = await next();
            if (result is not null)
            {
                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheAttr.DurationMinutes));
                options.ExpirationTokens.Add(new CancellationChangeToken(tokenStore.GetToken(cacheAttr.KeyPrefix)));

                cache.Set(key, result, options);
            }
            return result;
        }

        var commandResult = await next();
        foreach (var attr in typeof(TRequest).GetCustomAttributes(typeof(InvalidateCacheAttribute), false))
        {
            tokenStore.Invalidate(((InvalidateCacheAttribute)attr).Category);
        }
        return commandResult;
    }

    private static string BuildKey(string prefix, object request, Guid userId)
    {
        var values = request.GetType().GetProperties()
            .OrderBy(p => p.Name)
            .Select(p => p.GetValue(request) switch
            {
                null => "",
                Guid[] ids => string.Join(",", ids.OrderBy(id => id.ToString())),
                Guid id => id.ToString(),
                string s => s,
                var v => v.ToString() ?? ""
            });
        return $"{prefix}:{userId}:" + string.Join(":", values);
    }
}
```

- [ ] **Step 2: Commit**

---

### Task 4: Add User-Aware Filtering to Company Query Handlers

**Files:**
- Modify: `backend/Application/Companies/GetCompaniesQueryHandler.cs`
- Modify: `backend/Application/Companies/GetCompanyByIdQueryHandler.cs`

- [ ] **Step 1: Update GetCompaniesQueryHandler.cs**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompaniesQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompaniesQuery, List<CompanyResponse>>
{
    public async Task<List<CompanyResponse>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Companies.AsQueryable();

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(c => userContext.CompanyIds.Contains(c.Id));

        if (request.CompanyIds != null && request.CompanyIds.Length > 0)
            query = query.Where(c => request.CompanyIds.Contains(c.Id));

        var companies = await query.ToListAsync(cancellationToken);
        return [.. companies.Select(CompanyResponse.FromDomain)];
    }
}
```

- [ ] **Step 2: Update GetCompanyByIdQueryHandler.cs**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompanyByIdQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanyByIdQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var query = db.Companies.AsNoTracking().Where(c => c.Id == request.Id);

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(c => userContext.CompanyIds.Contains(c.Id));

        var company = await query.FirstOrDefaultAsync(cancellationToken);
        return company == null ? null : CompanyResponse.FromDomain(company);
    }
}
```

- [ ] **Step 3: Commit**

---

### Task 5: Add User-Aware Filtering to Site Query Handlers

**Files:**
- Modify: `backend/Application/Sites/GetSitesQueryHandler.cs`
- Modify: `backend/Application/Sites/GetSiteByIdQueryHandler.cs`

- [ ] **Step 1: Update GetSitesQueryHandler.cs**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSitesQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetSitesQuery, List<SiteResponse>>
{
    public async Task<List<SiteResponse>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Sites.Include(s => s.Companies).AsQueryable();

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(s => s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id)));

        if (request.Sites != null && request.Sites.Length != 0)
            query = query.Where(s => request.Sites.Contains(s.Id));

        return [.. query.Select(SiteResponse.FromDomain)];
    }
}
```

- [ ] **Step 2: Update GetSiteByIdQueryHandler.cs**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSiteByIdQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetSiteByIdQuery, SiteResponse?>
{
    public async Task<SiteResponse?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var query = db.Sites.Include(s => s.Companies).Where(s => s.Id == request.Id);

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(s => s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id)));

        var site = await query.FirstOrDefaultAsync(cancellationToken);
        return site == null ? null : SiteResponse.FromDomain(site);
    }
}
```

- [ ] **Step 3: Commit**

---

### Task 6: Add User-Aware Filtering to User Query Handlers

**Files:**
- Modify: `backend/Application/Users/GetUsersQueryHandler.cs`
- Modify: `backend/Application/Users/GetUserByIdQueryHandler.cs`

- [ ] **Step 1: Update GetUsersQueryHandler.cs**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUsersQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.Include(u => u.Companies).AsQueryable();

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(u => u.Id == userContext.UserId);

        if (request.UserIds != null && request.UserIds.Length > 0)
            query = query.Where(u => request.UserIds.Contains(u.Id));

        return [.. query.Select(UserResponse.FromDomain)];
    }
}
```

- [ ] **Step 2: Update GetUserByIdQueryHandler.cs**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUserByIdQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetUserByIdQuery, UserResponse?>
{
    public async Task<UserResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (userContext.CompanyIds.Count > 0 && request.Id != userContext.UserId)
            return null;

        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        return user == null ? null : UserResponse.FromDomain(user);
    }
}
```

- [ ] **Step 3: Commit**

---

### Task 7: Add User-Aware Checks to Company Settings and Expense Mappings Handlers

**Files:**
- Modify: `backend/Application/CompanySettings/GetCompanySettingsQuery.cs`
- Modify: `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`
- Modify: `backend/Application/CompanyExpenseMappings/GetCompanyExpenseMappingsQuery.cs`
- Modify: `backend/Application/CompanyExpenseMappings/UpsertCompanyExpenseMappingsCommand.cs`

- [ ] **Step 1: Update GetCompanySettingsQuery.cs handler**

Replace the handler class:

```csharp
using Application.Users;

internal class GetCompanySettingsQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?>
{
    public async Task<CompanySettingsResponse?> Handle(GetCompanySettingsQuery request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            return null;

        var settings = await db.CompanySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        return settings == null ? null : CompanySettingsResponse.FromDomain(settings);
    }
}
```

(Add `using Application.Users;` at top of file)

- [ ] **Step 2: Update UpdateCompanySettingsCommand.cs handler**

Replace the handler class:

```csharp
using Application.Users;

internal class UpdateCompanySettingsCommandHandler(DashboardDbContext db, IUserContext userContext) : ICommandHandler<UpdateCompanySettingsCommand>
{
    public async Task Handle(UpdateCompanySettingsCommand request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            throw new UnauthorizedAccessException();

        var settings = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        if (settings == null)
        {
            settings = new CompanySettingsEntity
            {
                CompanyId = request.CompanyId,
                DefaultIncomeAccountName = request.DefaultIncomeAccountName,
                PrimaryColor = request.PrimaryColor,
                NeutralColor = request.NeutralColor,
                ThemeMode = request.ThemeMode,
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
            settings.PrimaryColor = request.PrimaryColor;
            settings.NeutralColor = request.NeutralColor;
            settings.ThemeMode = request.ThemeMode;
            settings.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
```

(Add `using Application.Users;` at top. Add `UnauthorizedAccessException` is from `System` which is already available.)

- [ ] **Step 3: Update GetCompanyExpenseMappingsQuery.cs handler**

Replace the handler class:

```csharp
using Application.Users;

internal class GetCompanyExpenseMappingsQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanyExpenseMappingsQuery, List<CompanyExpenseMappingResponse>>
{
    public async Task<List<CompanyExpenseMappingResponse>> Handle(GetCompanyExpenseMappingsQuery request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            return [];

        return await db.CompanyExpenseMappings
            .Include(m => m.ExpenseType)
            .Where(m => m.CompanyId == request.CompanyId)
            .Select(m => CompanyExpenseMappingResponse.FromDomain(m, m.ExpenseType.Name))
            .ToListAsync(ct);
    }
}
```

- [ ] **Step 4: Update UpsertCompanyExpenseMappingsCommand.cs handler**

Replace the handler class:

```csharp
using Application.Users;

internal class UpsertCompanyExpenseMappingsCommandHandler(DashboardDbContext db, IUserContext userContext) : ICommandHandler<UpsertCompanyExpenseMappingsCommand>
{
    public async Task Handle(UpsertCompanyExpenseMappingsCommand request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            throw new UnauthorizedAccessException();

        var existing = await db.CompanyExpenseMappings
            .Where(m => m.CompanyId == request.CompanyId)
            .ToListAsync(ct);

        db.CompanyExpenseMappings.RemoveRange(existing);

        var newMappings = request.Mappings.Select(m => new CompanyExpenseMapping
        {
            CompanyId = request.CompanyId,
            ExpenseTypeId = m.ExpenseTypeId,
            ErpnextAccountName = m.ErpnextAccountName,
        }).ToList();

        await db.CompanyExpenseMappings.AddRangeAsync(newMappings, ct);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Commit**

---

### Task 8: Add InvalidateCache to User-Company Link Commands

**Files:**
- Modify: `backend/Application/Users/AddUserToCompanyCommand.cs`
- Modify: `backend/Application/Users/RemoveUserFromCompanyCommand.cs`

- [ ] **Step 1: Add InvalidateCache attribute to AddUserToCompanyCommand.cs**

Add `using Application.Caching;` at top, then change the record declaration:

```csharp
using Application.Abstractions;
using Application.Caching;
using FluentValidation;

namespace Application.Users;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record AddUserToCompanyCommand(
    Guid UserId,
    Guid CompanyId
) : ICommand;
```

- [ ] **Step 2: Add InvalidateCache attribute to RemoveUserFromCompanyCommand.cs**

```csharp
using Application.Abstractions;
using Application.Caching;
using FluentValidation;

namespace Application.Users;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record RemoveUserFromCompanyCommand(
    Guid UserId,
    Guid CompanyId
) : ICommand;
```

- [ ] **Step 3: Commit**

---

### Task 9: Update Test Infrastructure for IUserContext

**Files:**
- Modify: `backend/Tests/TestAuthHandler.cs`
- Modify: `backend/Tests/IntegrationTestFactory.cs`

- [ ] **Step 1: Add custom namespace claim to TestAuthHandler.cs**

In `TestAuthHandler.cs`, add the `https://meta.dashboard.com/user_id` claim to the claims array (after the scope claim):

```csharp
new Claim("https://meta.dashboard.com/user_id", Guid.NewGuid().ToString()),
```

This ensures each test run gets a unique user ID. Since this handler creates a new principal on every request, the middleware will get a claim but won't find the user in the DB (no user created in tests). The middleware will gracefully skip population (since `FirstOrDefaultAsync` returns null), leaving `UserContext` with default empty values.

- [ ] **Step 2: Register IUserContext in IntegrationTestFactory.cs**

In `IntegrationTestFactory.cs`, in `ConfigureServices`, add after the host services removal:

```csharp
// Register IUserContext for tests (stays empty — handlers skip filtering when CompanyIds is empty)
services.AddScoped<Application.Users.UserContext>();
services.AddScoped<Application.Users.IUserContext>(sp => sp.GetRequiredService<Application.Users.UserContext>());
```

Add the import:
```csharp
using Application.Users;
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build backend/Tests/Tests.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

---

### Task 10: Update Caching Tests for New Key Format

**Files:**
- Modify: `backend/Tests/CachingTests.cs`

- [ ] **Step 1: Update cache key assertions in CachingTests.cs**

The cache keys now include `:{userId}:` in the format. Since `IUserContext` is empty (Guid.Empty) in tests, keys will be e.g. `company:00000000-0000-0000-0000-000000000000:{guid}`.

Update lines 34, 57, 73, 87-88, 93, 106, 111, 123:

```csharp
// Line 34: GetCompanyById_CachesResponse test
var emptyGuid = Guid.Empty.ToString();
var cacheKey = $"company:{emptyGuid}:{companyId}";
Assert.True(cache.TryGetValue(cacheKey, out _), "Cache key should exist after first call");

// Line 57: GetCompanies_CachesWithQueryParams test
var emptyGuid = Guid.Empty.ToString();
var sortedIds = new[] { companyId, otherId }.OrderBy(id => id.ToString());
var expectedKey = $"companies:{emptyGuid}:{string.Join(",", sortedIds)}";
Assert.True(cache.TryGetValue(expectedKey, out _), "Composite cache key should exist");

// Line 73: DeleteCompany_InvalidatesCompanyCache test
var emptyGuid = Guid.Empty.ToString();
var cacheKey = $"company:{emptyGuid}:{companyId}";
Assert.False(cache.TryGetValue(cacheKey, out _), "Cache key should be evicted after delete");

// Lines 87-88: DeleteCompany_InvalidatesAllCategoryEntries test
var emptyGuid = Guid.Empty.ToString();
Assert.True(cache.TryGetValue($"company:{emptyGuid}:{companyId}", out _));
Assert.True(cache.TryGetValue($"companies:{emptyGuid}:", out _));

// Lines 93: DeleteCompany_InvalidatesAllCategoryEntries test (assert after delete)
Assert.False(cache.TryGetValue($"company:{emptyGuid}:{companyId}", out _));
Assert.False(cache.TryGetValue($"companies:{emptyGuid}:", out _));

// Line 106: UpdateSettings_InvalidatesSettingsCache test
Assert.True(cache.TryGetValue($"settings:{emptyGuid}:{companyId}", out _));

// Line 111: UpdateSettings_InvalidatesSettingsCache test (assert after update)
Assert.False(cache.TryGetValue($"settings:{emptyGuid}:{companyId}", out _),
    "Settings cache should be evicted after update");

// Line 123: GetExpenseTypes_CachesWithParameterlessQuery test
var emptyGuid = Guid.Empty.ToString();
Assert.True(cache.TryGetValue($"expense_types:{emptyGuid}:", out _), "Parameterless query should cache with empty value segment");
```

- [ ] **Step 2: Run caching tests**

Run: `dotnet test backend/Tests/Tests.csproj --filter "FullyQualifiedName~CachingTests"`
Expected: All 7 tests pass.

- [ ] **Step 3: Run all tests**

Run: `dotnet test backend/Tests/Tests.csproj`
Expected: All tests pass.

- [ ] **Step 4: Commit**

---

### Task 11: Fix CORS (Move Before Auth + Restrict Origins)

**Files:**
- Modify: `backend/Endpoints/DependancyInjection.cs`

- [ ] **Step 1: Restrict CORS origins in AddApi method**

In `backend/Endpoints/DependancyInjection.cs`, in the `AddApi` method, change the CORS policy:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

- [ ] **Step 2: Verify the MapApi method already has UseCors before auth**

The `MapApi` method was already updated in Task 2 Step 2 to have `app.UseCors("AllowFrontend")` before `app.UseAuthentication()`. Verify this is correct.

- [ ] **Step 3: Commit**

---

### Task 12: Add SecurityHeadersMiddleware

**Files:**
- Create: `backend/Host/Middleware/SecurityHeadersMiddleware.cs`
- Modify: `backend/Host/Program.cs`

- [ ] **Step 1: Write SecurityHeadersMiddleware.cs**

```csharp
namespace Host.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000";

        await _next(context);
    }
}
```

- [ ] **Step 2: Register in Program.cs**

In `backend/Host/Program.cs`, add after `app.UseMiddleware<GlobalExceptionMiddleware>();`:

```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
```

- [ ] **Step 3: Commit**

---

### Task 13: Remove Auth0 Token Log + Fix CreateUser Permission

**Files:**
- Modify: `backend/Infrastructure/Auth0/Auth0UserProvisioner.cs`
- Modify: `backend/Endpoints/Endpoints/UsersEndpoints.cs`

- [ ] **Step 1: Remove token log from Auth0UserProvisioner.cs**

Change line 70 from:
```csharp
logger.LogInformation($"Obtained Auth0 management API token: {token.AccessToken}");
```
to:
```csharp
logger.LogInformation("Obtained Auth0 management API token");
```

- [ ] **Step 2: Fix CreateUser permission in UsersEndpoints.cs**

Change line 47 from:
```csharp
.RequireAuthorization(Permissions.UpdateUsers);
```
to:
```csharp
.RequireAuthorization(Permissions.CreateUsers);
```

- [ ] **Step 3: Commit**

---

### Task 14: Frontend — Remove Client-Side Company Filtering

**Files:**
- Modify: `frontend/src/stores/AuthStore.ts`

- [ ] **Step 1: Update AuthStore.ts update() function**

In `frontend/src/stores/AuthStore.ts`, change the `update()` function around lines 109-118. Replace:

```typescript
      if (data?.companies?.length) {
        const allCompanies = await client.getUserCompanies();
        companies.value = allCompanies.filter(
          (c) => data.companies?.includes(c.id) ?? false
        );
```

With:

```typescript
      if (data?.companies?.length) {
        companies.value = await client.getUserCompanies();
```

(The backend now returns only the user's companies, so client-side filtering is redundant.)

- [ ] **Step 2: Commit**

---

### Task 15: Frontend — Wipe IndexedDB on User Change

**Files:**
- Modify: `frontend/src/services/cache/CachedApiClient.ts`
- Modify: `frontend/src/services/db/index.ts`

- [ ] **Step 1: Add userId to Dexie meta schema**

In `frontend/src/services/db/index.ts`, find the `MetaRow` interface/type and add `userId` field. Also bump `CURRENT_SCHEMA_VERSION`:

Change `CURRENT_SCHEMA_VERSION = 2` to `CURRENT_SCHEMA_VERSION = 3`.

Add `userId: string` to the meta row type. The existing meta table stores `{ key: "singleton", dbVersion, lastFullSync }`. Add `userId`:

```typescript
export interface MetaRow {
  key: string;
  dbVersion: number;
  lastFullSync: string;
  userId?: string;
}
```

- [ ] **Step 2: Update CachedApiClient.bootstrap() to check and store userId**

In `frontend/src/services/cache/CachedApiClient.ts`, modify the `bootstrap()` method.

First, add a helper to extract userId from the JWT token:

```typescript
  private getUserIdFromToken(): string | null {
    try {
      const authStore = (await import("@/stores/AuthStore")).useAuthStore();
      return authStore.userId || null;
    } catch {
      return null;
    }
  }
```

Wait, `bootstrap()` is not async for extracting the user ID this way. Let me restructure:

In `bootstrap()`, at the start, after getting `meta`:

```typescript
  async bootstrap(onProgress?: (current: number, total: number) => void): Promise<void> {
    const api = await this.ensureApi();
    const db = getCacheDB();
    const meta = await db.meta.get("singleton");

    // Check if user changed — wipe DB if different user
    const currentUserId = this.getCurrentUserId();
    if (currentUserId && meta?.userId && meta.userId !== currentUserId) {
      await db.delete();
      await db.open();
      // Re-fetch meta after delete (will be undefined)
    }

    if (meta?.dbVersion === CURRENT_SCHEMA_VERSION && meta?.userId === currentUserId) {
      return;
    }
```

And add the helper:

```typescript
  private getCurrentUserId(): string | null {
    try {
      const stores = (await import("@/stores/AuthStore")).useAuthStore;
      return stores().userId || null;
    } catch {
      return null;
    }
  }
```

Wait, `getCurrentUserId` needs `AuthStore` which requires Pinia to be active. In `App.vue`, `bootstrap()` is called after `authStore.update()` which sets `userId`. So `userId` should be available.

But `getCurrentUserId` is async because of the dynamic import. Let me make `bootstrap` handle this differently. Actually, let me keep it simple — pass userId as a parameter or read it from the auth store synchronously.

Looking at App.vue flow:
```
await authStore.update()  // sets userId
await CachedApiClient.getInstance().bootstrap()
```

Since `update()` runs first and sets `userId.value`, the Pinia store already has the value. I can import the store normally at the top of CachedApiClient.ts and read it.

But there's a circular dependency concern — AuthStore imports CachedApiClient. Let me check... AuthStore line 6: `import { CachedApiClient } from "@/services/cache/CachedApiClient";`

And I'd be adding `import { useAuthStore } from "@/stores/AuthStore";` to CachedApiClient. That's a circular import.

To avoid the circular dependency, I can:
1. Accept `userId` as a parameter to `bootstrap(userId?: string)`
2. Or read `userId` from localStorage directly

Option 1 is cleaner. Let me update the design:

In `CachedApiClient.bootstrap()`:
```typescript
async bootstrap(userId?: string, onProgress?: (current: number, total: number) => void): Promise<void> {
```

In `App.vue`, pass `userId`:
```typescript
await CachedApiClient.getInstance().bootstrap(authStore.userId)
```

In `CachedApiClient.bootstrap()`:
```typescript
async bootstrap(userId?: string, onProgress?: (current: number, total: number) => void): Promise<void> {
    const api = await this.ensureApi();
    const db = getCacheDB();
    const meta = await db.meta.get("singleton");

    // Wipe if user changed
    if (userId && meta?.userId && meta.userId !== userId) {
      await db.delete();
      await db.open();
      // Clear local meta ref since DB was wiped
    }

    if (meta?.dbVersion === CURRENT_SCHEMA_VERSION && meta?.userId === userId) {
      return;
    }
    // ... rest of bootstrap
    // Update meta at end:
    await db.meta.put({
      key: "singleton",
      dbVersion: CURRENT_SCHEMA_VERSION,
      lastFullSync: new Date().toISOString(),
      userId: userId || "",
    });
}
```

Also update `App.vue` to pass userId:

```typescript
await cachedClient.bootstrap(authStore.userId, (current: number, total: number) => {
  // existing progress tracking
});
```

Let me now write the steps for this task properly.

- [ ] **Step 1: Bump CURRENT_SCHEMA_VERSION and add userId to meta in db/index.ts (see description above)**  

- [ ] **Step 2: Update CachedApiClient.bootstrap() signature and user check**

In `frontend/src/services/cache/CachedApiClient.ts`, change the `bootstrap` method signature:

```typescript
async bootstrap(userId?: string, onProgress?: (current: number, total: number) => void): Promise<void> {
    const api = await this.ensureApi();
    const db = getCacheDB();
    const meta = await db.meta.get("singleton");

    if (userId && meta?.userId && meta.userId !== userId) {
      await db.delete();
      await db.open();
    }

    const refreshedMeta = await db.meta.get("singleton");
    if (refreshedMeta?.dbVersion === CURRENT_SCHEMA_VERSION && refreshedMeta?.userId === userId) {
      return;
    }

    const tasks: (() => Promise<void>)[] = [
      // ... same tasks as before
    ];

    let completed = 0;
    for (const task of tasks) {
      await task();
      completed++;
      onProgress?.(completed, tasks.length);
    }

    await db.meta.put({
      key: "singleton",
      dbVersion: CURRENT_SCHEMA_VERSION,
      lastFullSync: new Date().toISOString(),
      userId: userId || "",
    });
  }
```

- [ ] **Step 3: Update App.vue to pass userId**

In `frontend/src/App.vue`, change line 43 from:

```typescript
    await cacheClient.bootstrap();
```

to:

```typescript
    await cacheClient.bootstrap(authStore.userId);
```

- [ ] **Step 4: Build frontend to verify**

Run: `cd frontend && npm run build`
Expected: Build succeeds without errors.

- [ ] **Step 5: Commit**

---

### Task 16: Final Verification — Build All + Run All Tests

- [ ] **Step 1: Build backend**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 2: Run backend tests**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 3: Build frontend**

Run: `cd frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**
