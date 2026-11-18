using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;
using Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class FeaturesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public FeaturesTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public void SeedData_ExistsAfterMigration()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();

        Assert.Contains(db.Features, f => f.Code == "sales");
        Assert.Contains(db.Features, f => f.Code == "stock");
        Assert.Contains(db.Features, f => f.Code == "expense");
        Assert.Contains(db.Features, f => f.Code == "refund");
        Assert.Contains(db.Recipes, r => r.Name == "Retail");
        Assert.Contains(db.Recipes, r => r.Name == "Refund");
        Assert.Contains(db.Recipes, r => r.Name == "Service");
        var retailRecipeId = db.Recipes.Single(r => r.Name == "Retail").Id;
        var refundRecipeId = db.Recipes.Single(r => r.Name == "Refund").Id;
        var serviceRecipeId = db.Recipes.Single(r => r.Name == "Service").Id;
        var salesId = db.Features.Single(f => f.Code == "sales").Id;
        var stockId = db.Features.Single(f => f.Code == "stock").Id;
        var expenseId = db.Features.Single(f => f.Code == "expense").Id;
        var refundId = db.Features.Single(f => f.Code == "refund").Id;
        Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == retailRecipeId && rf.FeatureId == salesId));
        Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == retailRecipeId && rf.FeatureId == stockId));
        Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == retailRecipeId && rf.FeatureId == expenseId));
        Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == refundRecipeId && rf.FeatureId == refundId));
        Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == serviceRecipeId && rf.FeatureId == salesId));
        Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == serviceRecipeId && rf.FeatureId == expenseId));
    }

    [Fact]
    public async Task CreateFeature_Returns201_AndAppearsInList()
    {
        await ResetAsync();
        var request = new CreateFeatureRequest("test-feature", "Test Feature", "A test feature");

        var response = await _client.PostAsJsonAsync("/features", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();
        Assert.NotEqual(Guid.Empty, id);

        var list = await _client.GetFromJsonAsync<List<FeatureResponse>>("/features", TestJson.Options);
        Assert.NotNull(list);
        Assert.Contains(list, f => f.Id == id && f.Code == "test-feature" && f.Name == "Test Feature");
    }

    [Fact]
    public async Task CreateFeature_DuplicateCode_Returns409()
    {
        await ResetAsync();
        var request = new CreateFeatureRequest("sales", "Sales Copy", "duplicate of seeded code");

        var response = await _client.PostAsJsonAsync("/features", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateFeature_InvalidCode_Returns400()
    {
        await ResetAsync();
        var request = new CreateFeatureRequest("Stock Page", "Stock Page", "invalid code format");

        var response = await _client.PostAsJsonAsync("/features", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateFeature_ChangesNameNotCode()
    {
        await ResetAsync();
        var create = await _client.PostAsJsonAsync("/features", new CreateFeatureRequest("updatable", "Old Name", "old desc"));
        create.EnsureSuccessStatusCode();
        var id = await create.ReadCreatedIdAsync();

        var update = await _client.PutAsJsonAsync($"/features/{id}", new UpdateFeatureRequest("New Name", "New desc"));

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var list = await _client.GetFromJsonAsync<List<FeatureResponse>>("/features", TestJson.Options);
        Assert.NotNull(list);
        var feature = Assert.Single(list, f => f.Id == id);
        Assert.Equal("updatable", feature.Code);
        Assert.Equal("New Name", feature.Name);
        Assert.Equal("New desc", feature.Description);
    }

    [Fact]
    public async Task DeleteFeature_Unreferenced_Returns204_AndGoneFromList()
    {
        await ResetAsync();
        var create = await _client.PostAsJsonAsync("/features", new CreateFeatureRequest("throwaway", "Throwaway", "to delete"));
        create.EnsureSuccessStatusCode();
        var id = await create.ReadCreatedIdAsync();

        var delete = await _client.DeleteAsync($"/features/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await _client.GetFromJsonAsync<List<FeatureResponse>>("/features", TestJson.Options);
        Assert.NotNull(list);
        Assert.DoesNotContain(list, f => f.Id == id);
    }
}
