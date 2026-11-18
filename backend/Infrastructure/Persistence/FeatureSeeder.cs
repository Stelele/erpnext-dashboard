using Domain.Features;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class FeatureSeeder
{
    public static readonly Guid DefaultRecipeId = Guid.Parse("00000000-0000-0000-0002-000000000100");

    // Assigns the seeded "Default" recipe to every company that has no
    // recipe assignments yet. Idempotent: companies that already have
    // CompanyRecipe rows are left untouched.
    public static async Task SeedAsync(DashboardDbContext db, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == DefaultRecipeId, ct);
        if (recipe is null)
            return;

        var covered = await db.CompanyRecipes
            .Select(cr => cr.CompanyId)
            .Distinct()
            .ToListAsync(ct);

        var bare = await db.Companies
            .Where(c => !covered.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (bare.Count == 0)
            return;

        foreach (var companyId in bare)
        {
            db.CompanyRecipes.Add(new CompanyRecipe
            {
                CompanyId = companyId,
                RecipeId = DefaultRecipeId,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
