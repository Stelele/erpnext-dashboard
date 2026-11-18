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
        Assert.Contains(db.Features, f => f.Code == "expenses");
        Assert.Contains(db.Recipes, r => r.Name == "Default");
        var defaultRecipeId = db.Recipes.Single(r => r.Name == "Default").Id;
        var seededFeatureIds = db.Features
            .Where(f => f.Code == "sales" || f.Code == "stock" || f.Code == "expenses")
            .Select(f => f.Id)
            .ToList();
        Assert.Equal(3, seededFeatureIds.Count);
        foreach (var featureId in seededFeatureIds)
        {
            Assert.True(db.RecipeFeatures.Any(rf => rf.RecipeId == defaultRecipeId && rf.FeatureId == featureId));
        }
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
