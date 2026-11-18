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
public record CreateFeatureCommand(
    string Code,
    string Name,
    string Description
) : ICommand<Guid>;

public class CreateFeatureValidator : AbstractValidator<CreateFeatureCommand>
{
    public CreateFeatureValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-z][a-z0-9-]*$");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class CreateFeatureCommandHandler(DashboardDbContext db) : ICommandHandler<CreateFeatureCommand, Guid>
{
    public async Task<Guid> Handle(CreateFeatureCommand request, CancellationToken ct)
    {
        var codeExists = await db.Features
            .IgnoreQueryFilters()
            .AnyAsync(f => f.Code == request.Code, ct);

        if (codeExists)
            throw new DuplicateDomainMemberException($"A feature with the code '{request.Code}' already exists.");

        var feature = new Feature
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description ?? "",
        };

        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);

        return feature.Id;
    }
}
