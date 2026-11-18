using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public record GetCompanyRecipesQuery(Guid CompanyId) : IQuery<List<RecipeResponse>>;

public class GetCompanyRecipesQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanyRecipesQuery, List<RecipeResponse>>
{
    public async Task<List<RecipeResponse>> Handle(GetCompanyRecipesQuery request, CancellationToken ct)
    {
        var recipes = await db.CompanyRecipes
            .Where(cr => cr.CompanyId == request.CompanyId)
            .Select(cr => cr.Recipe)
            .ToListAsync(ct);

        var distinct = recipes.GroupBy(r => r.Id).Select(g => g.First()).ToList();

        var recipeIds = distinct.Select(r => r.Id).ToList();
        var featuresByRecipe = await RecipeFeatureQueries.LoadFeaturesByRecipeAsync(db, recipeIds, ct);

        return distinct.Select(r => new RecipeResponse(
            r.Id,
            r.Name,
            r.Description,
            featuresByRecipe.TryGetValue(r.Id, out var features) ? features : []
        )).ToList();
    }
}
