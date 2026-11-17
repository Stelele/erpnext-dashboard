using Application.Abstractions;
using Application.Caching;
using Domain.Exceptions;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
public record UpdateFeatureCommand(
    Guid Id,
    string Name,
    string Description
) : ICommand;

public class UpdateFeatureValidator : AbstractValidator<UpdateFeatureCommand>
{
    public UpdateFeatureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class UpdateFeatureCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateFeatureCommand>
{
    public async Task Handle(UpdateFeatureCommand request, CancellationToken ct)
    {
        var feature = await db.Features
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct)
            ?? throw new NotFoundException($"Feature not found: {request.Id}");

        feature.Name = request.Name;
        feature.Description = request.Description;
        feature.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
