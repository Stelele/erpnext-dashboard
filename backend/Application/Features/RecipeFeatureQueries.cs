using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public static class RecipeFeatureQueries
{
    public static async Task<Dictionary<Guid, List<FeatureResponse>>> LoadFeaturesByRecipeAsync(
        DashboardDbContext db,
        List<Guid> recipeIds,
        CancellationToken ct)
    {
        var rows = await db.RecipeFeatures
            .Where(rf => recipeIds.Contains(rf.RecipeId))
            .Select(rf => new { rf.RecipeId, Feature = rf.Feature })
            .ToListAsync(ct);

        return rows
            .Where(x => x.Feature != null)
            .GroupBy(x => x.RecipeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => FeatureResponse.FromDomain(x.Feature!)).OrderBy(x => x.Code).ToList()
            );
    }
}
