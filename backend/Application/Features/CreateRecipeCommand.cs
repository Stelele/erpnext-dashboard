using Application.Abstractions;
using Application.Caching;
using Domain.Exceptions;
using Domain.Features;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record CreateRecipeCommand(
    string Name,
    string Description,
    Guid[] FeatureIds
) : ICommand<Guid>;

public class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class CreateRecipeCommandHandler(DashboardDbContext db) : ICommandHandler<CreateRecipeCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        var nameExists = await db.Recipes
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Name == request.Name, ct);

        if (nameExists)
            throw new DuplicateDomainMemberException($"A recipe with the name '{request.Name}' already exists.");

        var featureIds = request.FeatureIds.Distinct().ToArray();

        var existing = await db.Features
            .Where(f => featureIds.Contains(f.Id))
            .Select(f => f.Id)
            .ToListAsync(ct);

        var unknown = featureIds.Except(existing).ToArray();

        if (unknown.Length > 0)
            throw new ValidationException($"Unknown feature ids: {string.Join(", ", unknown)}");

        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
        };

        db.Recipes.Add(recipe);

        foreach (var featureId in featureIds)
        {
            db.RecipeFeatures.Add(new RecipeFeature
            {
                RecipeId = recipe.Id,
                FeatureId = featureId,
            });
        }

        await db.SaveChangesAsync(ct);

        return recipe.Id;
    }
}
