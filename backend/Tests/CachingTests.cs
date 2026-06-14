using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Application.Requests;
using Microsoft.Extensions.Caching.Memory;

namespace Tests;

public class CachingTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public CachingTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static readonly string EmptyGuid = Guid.Empty.ToString();

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GetCompanyById_CachesResponse_AndReturnsCachedOnSecondCall()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("CachedCo");

        var sw1 = Stopwatch.StartNew();
        var resp1 = await _client.GetAsync($"/companies/{companyId}");
        sw1.Stop();
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        var cacheKey = $"company:{EmptyGuid}:{companyId}";
        Assert.True(cache.TryGetValue(cacheKey, out _), "Cache key should exist after first call");

        var sw2 = Stopwatch.StartNew();
        var resp2 = await _client.GetAsync($"/companies/{companyId}");
        sw2.Stop();
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds,
            "Second call should not be slower than first call");
    }

    [Fact]
    public async Task GetCompanies_CachesWithQueryParams()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("ParamCo");
        var otherId = await CreateCompanyAsync("OtherCo");

        var resp = await _client.GetAsync($"/companies?companyIds={companyId}&companyIds={otherId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        var sortedIds = new[] { companyId, otherId }.OrderBy(id => id.ToString());
        var expectedKey = $"companies:{EmptyGuid}:{string.Join(",", sortedIds)}";
        Assert.True(cache.TryGetValue(expectedKey, out _), "Composite cache key should exist");
    }

    [Fact]
    public async Task DeleteCompany_InvalidatesCompanyCache()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("InvalidateMe");

        await _client.GetAsync($"/companies/{companyId}");

        var deleteResp = await _client.DeleteAsync($"/companies/{companyId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        var cacheKey = $"company:{EmptyGuid}:{companyId}";
        Assert.False(cache.TryGetValue(cacheKey, out _), "Cache key should be evicted after delete");
    }

    [Fact]
    public async Task DeleteCompany_InvalidatesAllCategoryEntries()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("CatCo");

        await _client.GetAsync($"/companies/{companyId}");
        await _client.GetAsync($"/companies");

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        Assert.True(cache.TryGetValue($"company:{EmptyGuid}:{companyId}", out _));
        Assert.True(cache.TryGetValue($"companies:{EmptyGuid}:", out _));

        await _client.DeleteAsync($"/companies/{companyId}");

        Assert.False(cache.TryGetValue($"company:{EmptyGuid}:{companyId}", out _));
        Assert.False(cache.TryGetValue($"companies:{EmptyGuid}:", out _));
    }

    [Fact]
    public async Task UpdateSettings_InvalidatesSettingsCache()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("SettingsCo");

        await _client.GetAsync($"/api/companies/{companyId}/settings");

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        Assert.True(cache.TryGetValue($"settings:{EmptyGuid}:{companyId}", out _));

        var update = new { DefaultIncomeAccountName = "NewAccount" };
        await _client.PutAsJsonAsync($"/api/companies/{companyId}/settings", update);

        Assert.False(cache.TryGetValue($"settings:{EmptyGuid}:{companyId}", out _),
            "Settings cache should be evicted after update");
    }

    [Fact]
    public async Task GetExpenseTypes_CachesWithParameterlessQuery()
    {
        await ResetAsync();

        var resp = await _client.GetAsync("/api/expense-types");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        Assert.True(cache.TryGetValue($"expense_types:{EmptyGuid}:", out _), "Parameterless query should cache with empty value segment");
    }

    [Fact]
    public async Task EndpointWithoutCacheAttribute_BypassesPipelineCache()
    {
        await ResetAsync();

        var resp = await _client.GetAsync($"/sites/{Guid.NewGuid()}/logo?company=TestCo");
        Assert.True(resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK);

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        Assert.False(cache.TryGetValue($"logo:{Guid.Empty}", out _), "Logo endpoint should not use pipeline cache");
    }

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
}
