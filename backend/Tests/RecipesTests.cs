using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;

namespace Tests;

public class RecipesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public RecipesTests(IntegrationTestFactory factory)
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
        var response = await _client.PostAsJsonAsync("/recipes", new CreateRecipeRequest(name, $"{name} desc", featureIds));
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }

    [Fact]
    public async Task CreateRecipe_WithSeededFeatureIds_Returns201_AndListIncludesFeatures()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");

        var response = await _client.PostAsJsonAsync("/recipes", new CreateRecipeRequest("Sales Recipe", "Recipe with sales", [salesId]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();
        Assert.NotEqual(Guid.Empty, id);

        var list = await _client.GetFromJsonAsync<List<RecipeResponse>>("/recipes", TestJson.Options);
        Assert.NotNull(list);
        var recipe = Assert.Single(list, r => r.Id == id);
        Assert.Equal("Sales Recipe", recipe.Name);
        Assert.Contains(recipe.Features, f => f.Code == "sales");
    }

    [Fact]
    public async Task CreateRecipe_UnknownFeatureId_Returns400()
    {
        await ResetAsync();
        var unknownId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync("/recipes", new CreateRecipeRequest("Bad Recipe", "Unknown feature", [unknownId]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipe_NullFeatureIds_Returns400()
    {
        await ResetAsync();

        var response = await _client.PostAsJsonAsync("/recipes", new CreateRecipeRequest("Null Ids Recipe", "No feature ids", null!));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipe_OmittedDescription_DefaultsEmpty()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");

        var response = await _client.PostAsJsonAsync("/recipes", new { name = "No Desc Recipe", featureIds = new[] { salesId } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();

        var list = await _client.GetFromJsonAsync<List<RecipeResponse>>("/recipes", TestJson.Options);
        Assert.NotNull(list);
        var recipe = Assert.Single(list, r => r.Id == id);
        Assert.Equal("", recipe.Description);
        Assert.Contains(recipe.Features, f => f.Code == "sales");
    }

    [Fact]
    public async Task UpdateRecipe_ReplacesFeatureSet()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var stockId = await GetSeededFeatureIdAsync("stock");
        var expensesId = await GetSeededFeatureIdAsync("expenses");
        var id = await CreateRecipeAsync("Swap Recipe", [salesId]);

        var update = await _client.PutAsJsonAsync($"/recipes/{id}", new UpdateRecipeRequest("Swap Recipe", "Updated desc", [stockId, expensesId]));

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var list = await _client.GetFromJsonAsync<List<RecipeResponse>>("/recipes", TestJson.Options);
        Assert.NotNull(list);
        var recipe = Assert.Single(list, r => r.Id == id);
        Assert.Equal(["expenses", "stock"], recipe.Features.Select(f => f.Code).Order().ToArray());
    }

    [Fact]
    public async Task DeleteFeature_ReferencedByRecipe_Returns409()
    {
        await ResetAsync();
        var create = await _client.PostAsJsonAsync("/features", new CreateFeatureRequest("recipe-linked", "Recipe Linked", "referenced by a recipe"));
        create.EnsureSuccessStatusCode();
        var featureId = await create.ReadCreatedIdAsync();
        await CreateRecipeAsync("Linking Recipe", [featureId]);

        var delete = await _client.DeleteAsync($"/features/{featureId}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteRecipe_Unreferenced_Returns204()
    {
        await ResetAsync();
        var salesId = await GetSeededFeatureIdAsync("sales");
        var id = await CreateRecipeAsync("Doomed Recipe", [salesId]);

        var delete = await _client.DeleteAsync($"/recipes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await _client.GetFromJsonAsync<List<RecipeResponse>>("/recipes", TestJson.Options);
        Assert.NotNull(list);
        Assert.DoesNotContain(list, r => r.Id == id);
    }
}
