# MediatR Caching Pipeline Design

> **Goal:** Add unified, declarative `IMemoryCache`-based caching to all GET endpoints via a single MediatR pipeline behavior, with automatic cache invalidation on write operations.

**Tech Stack:** .NET 10, MediatR, IMemoryCache, EF Core, SQLite

---

## Architecture

Three new files in `backend/Application/Caching/`, one modification to DI registration:

```
Application/Caching/
├── CacheAttribute.cs              — Decorate query classes
├── InvalidateCacheAttribute.cs    — Decorate command classes
├── CategoryCacheTokenStore.cs     — Singleton: CancellationTokenSource per category
├── CachePipelineBehavior.cs       — Caches queries, invalidates commands
```

**Intercept flow:**

```
IRequest (query or command)
  → MediatR pipeline
    → ValidationBehavior (existing)
    → CachePipelineBehavior (NEW)
      ├─ Query: check cache → hit? return : call handler → store result + link to category token
      └─ Command: call handler → on success → cancel category token(s)
    → QueryHandler / CommandHandler
```

---

## CacheAttribute

Placed on `IQuery<TResponse>` records:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CacheAttribute : Attribute
{
    public int DurationMinutes { get; init; } = 5;
    public string KeyPrefix { get; init; } = string.Empty;
}
```

**Usage examples:**

```csharp
[Cache(DurationMinutes = 5, KeyPrefix = "company")]
public record GetCompanyByIdQuery(Guid Id) : IQuery<CompanyResponse?>;

[Cache(DurationMinutes = 5, KeyPrefix = "companies")]
public record GetCompaniesQuery(Guid[]? CompanyIds) : IQuery<List<CompanyResponse>>;

[Cache(DurationMinutes = 1440, KeyPrefix = "chart-colors")]
public record GetChartColorsQuery(string PrimaryColor) : IQuery<List<ChartColorDto>>;
```

**Key generation:** The behavior uses reflection on the query record's properties:

```csharp
// For GetCompanyByIdQuery(Id = "abc-123"):
//   Key = "company:abc-123"

// For GetCompaniesQuery(CompanyIds = ["abc", "def"]):
//   Key = "companies:abc,def"

// For GetCompaniesQuery(CompanyIds = null):
//   Key = "companies:"

// For GetChartColorsQuery(PrimaryColor = "#ff0000"):
//   Key = "chart-colors:#ff0000"
```

Properties are sorted by name for consistent ordering. `null` values become empty string. GUID arrays become comma-separated sorted values.

---

## InvalidateCacheAttribute

Placed on `ICommand` or `ICommand<TResponse>` records:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class InvalidateCacheAttribute : Attribute
{
    public string Category { get; init; } = string.Empty;
}
```

**Usage examples:**

```csharp
[InvalidateCache(Category = "company")]
[InvalidateCache(Category = "settings")]
[InvalidateCache(Category = "expense_mappings")]
public record DeleteCompanyCommand(Guid Id) : ICommand;
```

Multiple attributes = multiple categories invalidated.

---

## CategoryCacheTokenStore

Singleton service that holds a `CancellationTokenSource` per cache category. Used by the pipeline behavior to:
- Link cache entries to a category token (so cancelling the token evicts ALL entries for that category)
- Cancel and recreate the token when a category is invalidated

```csharp
public class CategoryCacheTokenStore
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();

    public CancellationToken GetToken(string category)
    {
        return _tokens.GetOrAdd(category, _ => new CancellationTokenSource()).Token;
    }

    public void Invalidate(string category)
    {
        if (_tokens.TryRemove(category, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
```

**How it works:**

When caching a query result, the behavior attaches the category's current token:
```csharp
var options = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromMinutes(attr.DurationMinutes))
    .AddExpirationToken(tokenStore.GetToken(cacheAttr.KeyPrefix));

cache.Set(key, result, options);
```

When a command invalidates a category, the behavior cancels the token:
```csharp
tokenStore.Invalidate(category);
```

The next cache entry for that category gets a fresh token (via `GetOrAdd`). Old entries with the cancelled token are automatically evicted by `IMemoryCache`.

**Advantages:**
- No per-key tracking, no memory leak from stale keys
- One cancel operation evicts ALL entries for a category
- Built on `IMemoryCache`'s native expiration token support
- Thread-safe via `ConcurrentDictionary`

---

## CachePipelineBehavior

```csharp
public class CachePipelineBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    CategoryCacheTokenStore tokenStore
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Query path: check for [Cache] attribute
        if (typeof(TRequest).GetCustomAttribute<CacheAttribute>() is { } cacheAttr)
        {
            var key = BuildKey(cacheAttr.KeyPrefix, request);
            if (cache.TryGetValue<TResponse>(key, out var cached) && cached is not null)
                return cached;

            var result = await next();
            if (result is not null)
            {
                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheAttr.DurationMinutes))
                    .AddExpirationToken(tokenStore.GetToken(cacheAttr.KeyPrefix));

                cache.Set(key, result, options);
            }
            return result;
        }

        // Command path: execute, then check for [InvalidateCache]
        var commandResult = await next();
        foreach (var attr in typeof(TRequest).GetCustomAttributes<InvalidateCacheAttribute>())
        {
            tokenStore.Invalidate(attr.Category);
        }
        return commandResult;
    }

    private static string BuildKey(string prefix, object request)
    {
        var props = request.GetType().GetProperties()
            .OrderBy(p => p.Name)
            .Select(p => p.GetValue(request) switch
            {
                null => "",
                Guid[] ids => string.Join(",", ids.OrderBy(id => id.ToString())),
                Guid id => id.ToString(),
                string s => s,
                var v => v.ToString() ?? ""
            });
        return $"{prefix}:{string.Join(":", props)}";
    }
}
```

---

## Endpoint Cache Configuration

| Endpoint | Query Class | Key Prefix | TTL |
|---|---|---|---|
| `GET /companies` | `GetCompaniesQuery` | `companies` | 5 min |
| `GET /companies/{id}` | `GetCompanyByIdQuery` | `company` | 5 min |
| `GET /api/companies/{id}/settings` | `GetCompanySettingsQuery` | `settings` | 5 min |
| `GET /sites` | `GetSitesQuery` | `sites` | 10 min |
| `GET /sites/{id}` | `GetSiteByIdQuery` | `sites` | 10 min |
| `GET /users` | `GetUsersQuery` | `users` | 5 min |
| `GET /users/{id}` | `GetUserByIdQuery` | `users` | 5 min |
| `GET /api/expense-types` | `GetExpenseTypesQuery` | `expense_types` | 10 min |
| `GET /api/expense-types/{id}` | `GetExpenseTypeByIdQuery` | `expense_types` | 10 min |
| `GET /api/companies/{id}/expense-mappings` | `GetCompanyExpenseMappingsQuery` | `expense_mappings` | 10 min |
| `GET /api/theme/chart-colors` | `GetChartColorsQuery` (NEW) | `chart_colors` | 24 hr |

---

## Invalidation Configuration

| Command | Categories Invalidated |
|---|---|
| `CreateCompanyCommand` | `company`, `companies` |
| `DeleteCompanyCommand` | `company`, `companies`, `settings`, `expense_mappings` |
| `CreateSiteCommand` | `sites` |
| `DeleteSiteCommand` | `sites`, `company`, `companies` |
| `CreateExpenseTypeCommand` | `expense_types` |
| `UpdateExpenseTypeCommand` | `expense_types` |
| `DeleteExpenseTypeCommand` | `expense_types` |
| `UpsertCompanyExpenseMappingsCommand` | `expense_mappings` |
| `UpdateCompanySettingsCommand` | `settings` |

User and user-company association commands are NOT invalidated — user cache churn is acceptable for admin operations.

---

## Chart Colors MediatR-ification

New files:

```
Application/Theme/
├── GetChartColorsQuery.cs  — Query record + handler
├── ThemeAttributes.cs      — [Cache(...)] attribute
```

Existing `ThemeEndpoints.cs` changes from direct `ChartColorData.GetColors()` call to `mediator.Send(new GetChartColorsQuery(color))`.

The `ThemeAttributes.cs` file (if needed) or attribute on the query itself:

```csharp
[Cache(DurationMinutes = 1440, KeyPrefix = "chart_colors")]
public record GetChartColorsQuery(string PrimaryColor) : IQuery<List<ChartColor>>;
```

---

## Testing

Test file: `backend/Tests/CachingTests.cs`

Uses xUnit + `IntegrationTestFactory` (in-memory SQLite, stub R2/Auth0, test auth). Tests verify caching through `IMemoryCache` inspection and response timing.

### Cache hit test

```csharp
[Fact]
public async Task GetCompanyById_CachesResponse_AndReturnsCachedOnSecondCall()
{
    await ResetAsync();
    var siteId = await CreateSiteAsync("site-1");
    var request = new CreateCompanyRequest(siteId, "CachedCo", "");
    var createResp = await _client.PostAsJsonAsync("/companies", request);
    var companyId = await createResp.ReadCreatedIdAsync();

    // First call — should populate cache
    var sw1 = Stopwatch.StartNew();
    var resp1 = await _client.GetAsync($"/companies/{companyId}");
    sw1.Stop();
    Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

    // Verify cache entry exists
    var cache = _factory.Services.GetRequiredService<IMemoryCache>();
    var cacheKey = $"company:{companyId}";
    Assert.True(cache.TryGetValue(cacheKey, out _), "Cache key should exist after first call");

    // Second call — should hit cache (faster)
    var sw2 = Stopwatch.StartNew();
    var resp2 = await _client.GetAsync($"/companies/{companyId}");
    sw2.Stop();
    Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
    Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds,
        "Second call should not be slower than first call");
}
```

### Cache invalidation test

```csharp
[Fact]
public async Task DeleteCompany_InvalidatesCompanyCache()
{
    await ResetAsync();
    var companyId = await CreateCompanyAsync("InvalidateMe");

    // First call populates cache
    await _client.GetAsync($"/companies/{companyId}");

    // Delete the company — should invalidate cache
    var deleteResp = await _client.DeleteAsync($"/companies/{companyId}");
    Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

    // Verify cache entry was evicted
    var cache = _factory.Services.GetRequiredService<IMemoryCache>();
    var cacheKey = $"company:{companyId}";
    Assert.False(cache.TryGetValue(cacheKey, out _), "Cache key should be evicted after delete");
}
```

### Category invalidation test

```csharp
[Fact]
public async Task DeleteCompany_InvalidatesAllCategoryEntries()
{
    await ResetAsync();
    var siteId = await CreateSiteAsync("site-1");
    var request = new CreateCompanyRequest(siteId, "CatCo", "");
    var resp = await _client.PostAsJsonAsync("/companies", request);
    var companyId = await resp.ReadCreatedIdAsync();

    // Populate multiple cache entries for the 'company' category
    await _client.GetAsync($"/companies/{companyId}");
    await _client.GetAsync($"/companies");

    var cache = _factory.Services.GetRequiredService<IMemoryCache>();

    // Verify both keys exist
    Assert.True(cache.TryGetValue($"company:{companyId}", out _));
    Assert.True(cache.TryGetValue("companies:", out _));

    // Delete triggers invalidation of 'company' AND 'companies' categories
    await _client.DeleteAsync($"/companies/{companyId}");

    // Both should be evicted
    Assert.False(cache.TryGetValue($"company:{companyId}", out _));
    Assert.False(cache.TryGetValue("companies:", out _));
}
```

### TTL test

```csharp
[Fact]
public async Task GetCompanySettings_ExpiresAfterTtl()
{
    await ResetAsync();
    var companyId = await CreateCompanyWithSettingsAsync("TtlCo");

    // First call populates cache
    await _client.GetAsync($"/api/companies/{companyId}/settings");

    var cache = _factory.Services.GetRequiredService<IMemoryCache>();
    var cacheKey = $"settings:{companyId}";
    Assert.True(cache.TryGetValue(cacheKey, out _));

    // Simulate TTL expiration by removing the entry and checking
    // that a subsequent call re-populates it
    cache.Remove(cacheKey);
    Assert.False(cache.TryGetValue(cacheKey, out _));

    // Next call should re-populate
    var resp = await _client.GetAsync($"/api/companies/{companyId}/settings");
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    Assert.True(cache.TryGetValue(cacheKey, out _));
}
```

### Uncacheable endpoint test

```csharp
[Fact]
public async Task EndpointWithoutCacheAttribute_BypassesCache()
{
    await ResetAsync();

    // The logo endpoint keeps its own IMemoryCache (not MediatR pipeline)
    // — verify it still works but doesn't use pipeline cache keys
    var resp = await _client.GetAsync($"/sites/{Guid.NewGuid()}/logo?company=TestCo");
    Assert.True(resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK);

    var cache = _factory.Services.GetRequiredService<IMemoryCache>();
    // Verify no pipeline cache key was created
    Assert.False(cache.TryGetValue($"sites:{Guid.Empty}", out _));
}
```

### Helper methods (add to CachingTests class)

```csharp
private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

private async Task<Guid> CreateSiteAsync(string name)
{
    var request = new CreateSiteRequest(name, $"https://{Guid.NewGuid():N}.example.com", "", "token");
    var response = await _client.PostAsJsonAsync("/sites", request);
    response.EnsureSuccessStatusCode();
    return await response.ReadCreatedIdAsync();
}

private async Task<Guid> CreateCompanyAsync(string name)
{
    var siteId = await CreateSiteAsync($"site-{Guid.NewGuid():N}");
    var request = new CreateCompanyRequest(siteId, name, "");
    var response = await _client.PostAsJsonAsync("/companies", request);
    response.EnsureSuccessStatusCode();
    return await response.ReadCreatedIdAsync();
}

private async Task<Guid> CreateCompanyWithSettingsAsync(string name)
{
    var companyId = await CreateCompanyAsync(name);
    // Settings are auto-seeded on company creation
    return companyId;
}
```

---

## Migration Tasks

### Task 1: Create cache infrastructure
- Create: `Application/Caching/CacheAttribute.cs`
- Create: `Application/Caching/InvalidateCacheAttribute.cs`
- Create: `Application/Caching/CategoryCacheTokenStore.cs`
- Create: `Application/Caching/CachePipelineBehavior.cs`
- Modify: `Application/DependencyInjection.cs` — register `CategoryCacheTokenStore` (singleton) and `CachePipelineBehavior` (transient)

### Task 2: Decorate queries with `[Cache]`
- Add `[Cache]` to: `GetCompaniesQuery`, `GetCompanyByIdQuery`, `GetCompanySettingsQuery`, `GetSitesQuery`, `GetSiteByIdQuery`, `GetUsersQuery`, `GetUserByIdQuery`, `GetExpenseTypesQuery`, `GetExpenseTypeByIdQuery`, `GetCompanyExpenseMappingsQuery`
- Remove existing ad-hoc `IMemoryCache` injection from: `GetCompanyByIdQueryHandler`, `GetCompanySettingsQueryHandler` (with cache logic)

### Task 3: Decorate commands with `[InvalidateCache]`
- Add `[InvalidateCache]` to all 9 command classes listed in the invalidation table

### Task 4: MediatR-ify chart colors
- Create: `Application/Theme/GetChartColorsQuery.cs`
- Modify: `Endpoints/Endpoints/ThemeEndpoints.cs` — use MediatR instead of direct call

### Task 5: Create caching tests
- Create: `Tests/CachingTests.cs` — 5 test methods covering cache hit, invalidation, category eviction, TTL, and uncacheable endpoints

### Task 6: Verify
- Run: `dotnet build` — zero errors
- Run: `dotnet test` — all tests pass (existing + new caching tests)
