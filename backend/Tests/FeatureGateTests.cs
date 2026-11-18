using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;

namespace Tests;

public class FeatureGateTests : IClassFixture<IntegrationTestFactory>
{
    private static readonly Guid DefaultRecipeId = Guid.Parse("00000000-0000-0000-0002-000000000100");

    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public FeatureGateTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    private async Task<Guid> GetSeededFeatureIdAsync(string code)
    {
        var features = await _client.GetFromJsonAsync<List<FeatureResponse>>("/features", TestJson.Options);
        Assert.NotNull(features);
        return Assert.Single(features, f => f.Code == code).Id;
    }

    private async Task<Guid> CreateRecipeAsync(string name, Guid[] featureIds)
    {
        var uniqueName = $"{name} {Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/recipes", new CreateRecipeRequest(uniqueName, $"{uniqueName} desc", featureIds));
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }

    private async Task<Guid> CreateSiteAsync()
    {
        var request = new CreateSiteRequest($"Site-{Guid.NewGuid():N}", $"https://{Guid.NewGuid():N}.example.com", "", "token");
        var response = await _client.PostAsJsonAsync("/sites", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }

    private async Task<Guid> CreateCompanyAsync(string name, Guid[]? recipeIds = null)
    {
        var siteId = await CreateSiteAsync();
        var request = new CreateCompanyRequest(siteId, name, "", recipeIds);
        var response = await _client.PostAsJsonAsync("/companies", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }

    [Fact]
    public async Task ExpenseMappings_CompanyWithoutExpenses_Returns404()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var salesOnlyRecipe = await CreateRecipeAsync("Sales Only", [salesId]);
        var companyId = await CreateCompanyAsync("No Expenses Co", [salesOnlyRecipe]);

        var getResponse = await _client.GetAsync($"/api/companies/{companyId}/expense-mappings");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/companies/{companyId}/expense-mappings",
            new UpsertCompanyExpenseMappingsRequest(Mappings: []));
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
    }

    [Fact]
    public async Task ExpenseMappings_CompanyWithExpenses_Returns200()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("Expenses Co", [DefaultRecipeId]);

        var getResponse = await _client.GetAsync($"/api/companies/{companyId}/expense-mappings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
