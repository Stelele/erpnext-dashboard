using Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class FeaturesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public FeaturesTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void SeedData_ExistsAfterMigration()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();

        Assert.Equal(3, db.Features.Count());
        var codes = db.Features.Select(f => f.Code).Order().ToList();
        Assert.Equal(["expenses", "sales", "stock"], codes);
        Assert.Contains(db.Recipes, r => r.Name == "Default");
        Assert.Equal(3, db.RecipeFeatures.Count());
    }
}
