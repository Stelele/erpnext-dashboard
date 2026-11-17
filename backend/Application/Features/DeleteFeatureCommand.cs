using Application.Abstractions;
using Application.Caching;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record DeleteFeatureCommand(Guid Id) : ICommand;

public class DeleteFeatureCommandHandler(DashboardDbContext db) : ICommandHandler<DeleteFeatureCommand>
{
    public async Task Handle(DeleteFeatureCommand request, CancellationToken ct)
    {
        var feature = await db.Features
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct)
            ?? throw new NotFoundException($"Feature not found: {request.Id}");

        var inUse = await db.RecipeFeatures.AnyAsync(rf => rf.FeatureId == request.Id, ct);

        if (inUse)
            throw new ResourceInUseException($"Feature '{feature.Code}' is referenced by a recipe and cannot be deleted.");

        feature.IsDeleted = true;
        feature.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
