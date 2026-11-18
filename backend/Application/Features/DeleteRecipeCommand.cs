using Application.Abstractions;
using Application.Caching;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record DeleteRecipeCommand(Guid Id) : ICommand;

public class DeleteRecipeCommandHandler(DashboardDbContext db) : ICommandHandler<DeleteRecipeCommand>
{
    public async Task Handle(DeleteRecipeCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException($"Recipe not found: {request.Id}");

        var inUse = await db.CompanyRecipes.AnyAsync(cr => cr.RecipeId == request.Id, ct);

        if (inUse)
            throw new ResourceInUseException($"Recipe '{recipe.Name}' is assigned to a company and cannot be deleted.");

        recipe.IsDeleted = true;
        recipe.UpdatedOn = DateTimeOffset.UtcNow;

        var joinRows = await db.RecipeFeatures.Where(rf => rf.RecipeId == request.Id).ToListAsync(ct);
        db.RecipeFeatures.RemoveRange(joinRows);

        await db.SaveChangesAsync(ct);
    }
}
