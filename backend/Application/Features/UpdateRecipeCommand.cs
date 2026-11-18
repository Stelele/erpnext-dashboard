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
public record UpdateRecipeCommand(
    Guid Id,
    string Name,
    string Description,
    Guid[] FeatureIds
) : ICommand;

public class UpdateRecipeValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);

        RuleFor(x => x.FeatureIds)
            .NotNull();
    }
}

public class UpdateRecipeCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateRecipeCommand>
{
    public async Task Handle(UpdateRecipeCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException($"Recipe not found: {request.Id}");

        var nameExists = await db.Recipes
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id != request.Id && r.Name == request.Name, ct);

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

        recipe.Name = request.Name;
        recipe.Description = request.Description ?? "";
        recipe.UpdatedOn = DateTimeOffset.UtcNow;

        var current = await db.RecipeFeatures
            .Where(rf => rf.RecipeId == request.Id)
            .ToListAsync(ct);

        var toRemove = current.Where(rf => !featureIds.Contains(rf.FeatureId)).ToList();
        db.RecipeFeatures.RemoveRange(toRemove);

        var currentIds = current.Select(rf => rf.FeatureId).ToHashSet();
        foreach (var featureId in featureIds.Where(id => !currentIds.Contains(id)))
        {
            db.RecipeFeatures.Add(new RecipeFeature
            {
                RecipeId = request.Id,
                FeatureId = featureId,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
