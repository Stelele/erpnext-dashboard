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
        var recipes = await db.Recipes.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

        var featuresByRecipe = await RecipeFeatureQueries.LoadFeaturesByRecipeAsync(db, recipes.Select(r => r.Id).ToList(), ct);

        return recipes.Select(r => new RecipeResponse(
            r.Id,
            r.Name,
            r.Description,
            featuresByRecipe.TryGetValue(r.Id, out var features) ? features : []
        )).ToList();
    }
}
