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

public class SetCompanyRecipesValidator : AbstractValidator<SetCompanyRecipesCommand>
{
    public SetCompanyRecipesValidator()
    {
        RuleFor(x => x.RecipeIds)
            .NotNull();
    }
}

public class SetCompanyRecipesCommandHandler(DashboardDbContext db) : ICommandHandler<SetCompanyRecipesCommand>
{
    public async Task Handle(SetCompanyRecipesCommand request, CancellationToken ct)
    {
        var companyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId, ct);

        if (!companyExists)
            throw new ValidationException($"Unknown company id: {request.CompanyId}");

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

        var toRemove = current.Where(cr => !recipeIds.Contains(cr.RecipeId)).ToList();
        db.CompanyRecipes.RemoveRange(toRemove);

        var currentIds = current.Select(cr => cr.RecipeId).ToHashSet();
        foreach (var recipeId in recipeIds.Where(id => !currentIds.Contains(id)))
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
