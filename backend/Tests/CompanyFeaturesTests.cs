using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;
using Infrastructure.Models;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class CompanyFeaturesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public CompanyFeaturesTests(IntegrationTestFactory factory)
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
    public async Task Union_AcrossTwoRecipes()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var stockId = await GetSeededFeatureIdAsync("stock");
        var recipeA = await CreateRecipeAsync("Recipe A", [salesId]);
        var recipeB = await CreateRecipeAsync("Recipe B", [stockId]);
        var companyId = await CreateCompanyAsync("Union Co");

        var set = await _client.PutAsJsonAsync($"/companies/{companyId}/recipes", new SetCompanyRecipesRequest([recipeA, recipeB]));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var features = await _client.GetFromJsonAsync<List<string>>($"/companies/{companyId}/features", TestJson.Options);
        Assert.NotNull(features);
        Assert.Equal(["sales", "stock"], features.Order().ToArray());
    }

    [Fact]
    public async Task LiveRecipeEdit_Propagates()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var expensesId = await GetSeededFeatureIdAsync("expenses");
        var recipeId = await CreateRecipeAsync("Live Recipe", [salesId]);
        var companyId = await CreateCompanyAsync("Live Co");

        var set = await _client.PutAsJsonAsync($"/companies/{companyId}/recipes", new SetCompanyRecipesRequest([recipeId]));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var update = await _client.PutAsJsonAsync($"/recipes/{recipeId}", new UpdateRecipeRequest("Live Recipe", "Updated desc", [salesId, expensesId]));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var features = await _client.GetFromJsonAsync<List<string>>($"/companies/{companyId}/features", TestJson.Options);
        Assert.NotNull(features);
        Assert.Equal(["expenses", "sales"], features.Order().ToArray());
    }

    [Fact]
    public async Task SetCompanyRecipes_ReplacesSet_AndDedupes()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var stockId = await GetSeededFeatureIdAsync("stock");
        var recipeA = await CreateRecipeAsync("Recipe A", [salesId]);
        var recipeB = await CreateRecipeAsync("Recipe B", [stockId]);
        var companyId = await CreateCompanyAsync("Dedupe Co");

        var set = await _client.PutAsJsonAsync($"/companies/{companyId}/recipes", new SetCompanyRecipesRequest([recipeA, recipeA, recipeB]));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var features = await _client.GetFromJsonAsync<List<string>>($"/companies/{companyId}/features", TestJson.Options);
        Assert.NotNull(features);
        Assert.Equal(["sales", "stock"], features.Order().ToArray());

        var recipes = await _client.GetFromJsonAsync<List<RecipeResponse>>($"/companies/{companyId}/recipes", TestJson.Options);
        Assert.NotNull(recipes);
        Assert.Equal(2, recipes.Count);

        var clear = await _client.PutAsJsonAsync($"/companies/{companyId}/recipes", new SetCompanyRecipesRequest([]));
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);

        var empty = await _client.GetFromJsonAsync<List<string>>($"/companies/{companyId}/features", TestJson.Options);
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task SetCompanyRecipes_UnknownRecipeId_Returns400()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("Unknown Co");

        var response = await _client.PutAsJsonAsync($"/companies/{companyId}/recipes", new SetCompanyRecipesRequest([Guid.NewGuid()]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetCompanyRecipes_UnknownCompanyId_Returns400()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var recipeId = await CreateRecipeAsync("Orphan Recipe", [salesId]);

        var response = await _client.PutAsJsonAsync($"/companies/{Guid.NewGuid()}/recipes", new SetCompanyRecipesRequest([recipeId]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetCompanyRecipes_NullRecipeIds_Returns400()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("Null Ids Co");

        var response = await _client.PutAsJsonAsync($"/companies/{companyId}/recipes", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCompany_UnknownRecipeIds_Returns400_AndLeavesNoPartialCompany()
    {
        await ResetAsync();
        var siteId = await CreateSiteAsync();
        var request = new CreateCompanyRequest(siteId, "Partial Co", "", [Guid.NewGuid()]);

        var response = await _client.PostAsJsonAsync("/companies", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var companies = await _client.GetFromJsonAsync<List<CompanyResponse>>("/companies", TestJson.Options);
        Assert.NotNull(companies);
        Assert.DoesNotContain(companies, c => c.Name == "Partial Co");
    }

    [Fact]
    public async Task CreateCompany_WithRecipeIds_AssignsThem()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var stockId = await GetSeededFeatureIdAsync("stock");
        var recipeA = await CreateRecipeAsync("Recipe A", [salesId]);
        var recipeB = await CreateRecipeAsync("Recipe B", [stockId]);

        var withRecipes = await CreateCompanyAsync("With Recipes Co", [recipeA, recipeB]);
        var features = await _client.GetFromJsonAsync<List<string>>($"/companies/{withRecipes}/features", TestJson.Options);
        Assert.NotNull(features);
        Assert.Equal(["sales", "stock"], features.Order().ToArray());

        var withoutRecipes = await CreateCompanyAsync("Without Recipes Co");
        var empty = await _client.GetFromJsonAsync<List<string>>($"/companies/{withoutRecipes}/features", TestJson.Options);
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task DeleteCompany_WithRecipes_Returns204()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var recipeId = await CreateRecipeAsync("Doomed Assign Recipe", [salesId]);
        var companyId = await CreateCompanyAsync("Doomed Co", [recipeId]);

        var delete = await _client.DeleteAsync($"/companies/{companyId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task GetCompanyRecipes_ReturnsAssignedRecipesWithFeatures()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var recipeId = await CreateRecipeAsync("Assigned Recipe", [salesId]);
        var companyId = await CreateCompanyAsync("Assigned Co", [recipeId]);

        var recipes = await _client.GetFromJsonAsync<List<RecipeResponse>>($"/companies/{companyId}/recipes", TestJson.Options);

        Assert.NotNull(recipes);
        var recipe = Assert.Single(recipes);
        Assert.Equal(recipeId, recipe.Id);
        Assert.Contains(recipe.Features, f => f.Code == "sales");
    }

    [Fact]
    public async Task Seeder_AssignsDefault_ToCompaniesWithoutRecipes()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var customRecipe = await CreateRecipeAsync("Custom Recipe", [salesId]);
        var bareCompany = await CreateCompanyAsync("Bare Co");
        var coveredCompany = await CreateCompanyAsync("Covered Co", [customRecipe]);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
            await FeatureSeeder.SeedAsync(db);
        }

        var bareFeatures = await _client.GetFromJsonAsync<List<string>>($"/companies/{bareCompany}/features", TestJson.Options);
        Assert.NotNull(bareFeatures);
        Assert.Equal(["expenses", "sales", "stock"], bareFeatures.Order().ToArray());

        var coveredFeatures = await _client.GetFromJsonAsync<List<string>>($"/companies/{coveredCompany}/features", TestJson.Options);
        Assert.NotNull(coveredFeatures);
        Assert.Equal(["sales"], coveredFeatures.Order().ToArray());
    }

    [Fact]
    public async Task GetCompanies_IncludesEffectiveFeatures()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var stockId = await GetSeededFeatureIdAsync("stock");
        var recipeId = await CreateRecipeAsync("List Recipe", [salesId, stockId]);
        var companyId = await CreateCompanyAsync("List Co", [recipeId]);
        var bareId = await CreateCompanyAsync("Bare List Co");

        var companies = await _client.GetFromJsonAsync<List<CompanyResponse>>("/companies", TestJson.Options);
        Assert.NotNull(companies);
        var company = Assert.Single(companies, c => c.Id == companyId);
        Assert.Equal(["sales", "stock"], company.Features.Order().ToArray());
        var bare = Assert.Single(companies, c => c.Id == bareId);
        Assert.Empty(bare.Features);
    }

    [Fact]
    public async Task GetCompanyById_IncludesEffectiveFeatures()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var recipeId = await CreateRecipeAsync("Single Recipe", [salesId]);
        var companyId = await CreateCompanyAsync("Single Co", [recipeId]);
        var bareId = await CreateCompanyAsync("Bare Single Co");

        var company = await _client.GetFromJsonAsync<CompanyResponse>($"/companies/{companyId}", TestJson.Options);
        Assert.NotNull(company);
        Assert.Equal(["sales"], company.Features.Order().ToArray());

        var bare = await _client.GetFromJsonAsync<CompanyResponse>($"/companies/{bareId}", TestJson.Options);
        Assert.NotNull(bare);
        Assert.Empty(bare.Features);
    }

    [Fact]
    public async Task RecipeEdit_InvalidatesCompaniesCache()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var expensesId = await GetSeededFeatureIdAsync("expenses");
        var recipeId = await CreateRecipeAsync("Cache Recipe", [salesId]);
        var companyId = await CreateCompanyAsync("Cache Co", [recipeId]);

        var before = await _client.GetFromJsonAsync<List<CompanyResponse>>("/companies", TestJson.Options);
        Assert.NotNull(before);
        Assert.Equal(["sales"], Assert.Single(before, c => c.Id == companyId).Features.Order().ToArray());

        var update = await _client.PutAsJsonAsync($"/recipes/{recipeId}", new UpdateRecipeRequest("Cache Recipe", "Updated desc", [salesId, expensesId]));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var after = await _client.GetFromJsonAsync<List<CompanyResponse>>("/companies", TestJson.Options);
        Assert.NotNull(after);
        Assert.Equal(["expenses", "sales"], Assert.Single(after, c => c.Id == companyId).Features.Order().ToArray());
    }
}
