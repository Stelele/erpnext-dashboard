using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public record GetRecipesQuery() : IQuery<List<RecipeResponse>>;

public class GetRecipesQueryHandler(DashboardDbContext db) : IQueryHandler<GetRecipesQuery, List<RecipeResponse>>
{
    public async Task<List<RecipeResponse>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        var recipes = await db.Recipes.OrderBy(r => r.Name).ToListAsync(ct);

        var featuresByRecipe = await LoadFeaturesByRecipeAsync(recipes.Select(r => r.Id).ToList(), ct);

        return recipes.Select(r => new RecipeResponse(
            r.Id,
            r.Name,
            r.Description,
            featuresByRecipe.TryGetValue(r.Id, out var features) ? features : []
        )).ToList();
    }

    internal async Task<Dictionary<Guid, List<FeatureResponse>>> LoadFeaturesByRecipeAsync(List<Guid> recipeIds, CancellationToken ct)
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
                g => g.Select(x => FeatureResponse.FromDomain(x.Feature!)).ToList()
            );
    }
}
