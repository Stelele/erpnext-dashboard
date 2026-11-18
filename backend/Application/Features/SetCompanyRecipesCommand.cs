using Application.Abstractions;
using Application.Caching;
using Domain.Features;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record SetCompanyRecipesCommand(
    Guid CompanyId,
    Guid[] RecipeIds
) : ICommand;

public class SetCompanyRecipesCommandHandler(DashboardDbContext db) : ICommandHandler<SetCompanyRecipesCommand>
{
    public async Task Handle(SetCompanyRecipesCommand request, CancellationToken ct)
    {
        var recipeIds = request.RecipeIds.Distinct().ToArray();

        var existing = await db.Recipes
            .Where(r => recipeIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var unknown = recipeIds.Except(existing).ToArray();

        if (unknown.Length > 0)
            throw new ValidationException($"Unknown recipe ids: {string.Join(", ", unknown)}");

        var current = await db.CompanyRecipes
            .Where(cr => cr.CompanyId == request.CompanyId)
            .ToListAsync(ct);

        db.CompanyRecipes.RemoveRange(current);

        foreach (var recipeId in recipeIds)
        {
            db.CompanyRecipes.Add(new CompanyRecipe
            {
                CompanyId = request.CompanyId,
                RecipeId = recipeId,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
