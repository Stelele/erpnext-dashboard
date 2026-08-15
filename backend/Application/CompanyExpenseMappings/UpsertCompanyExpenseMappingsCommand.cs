using Application.Abstractions;
using Application.Caching;
using Application.Users;
using Domain.CompanyExpenseMappings;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanyExpenseMappings;

[InvalidateCache(Category = "expense_mappings")]
public record UpsertCompanyExpenseMappingsCommand(
    Guid CompanyId,
    List<MappingItem> Mappings
) : ICommand;

public record MappingItem(Guid ExpenseTypeId, string ErpnextAccountName);

public sealed class UpsertCompanyExpenseMappingsCommandValidator : AbstractValidator<UpsertCompanyExpenseMappingsCommand>
{
    public UpsertCompanyExpenseMappingsCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty();

        RuleFor(x => x.Mappings)
            .NotEmpty();

        RuleForEach(x => x.Mappings).ChildRules(mapping =>
        {
            mapping.RuleFor(x => x.ExpenseTypeId)
                .NotEmpty();

            mapping.RuleFor(x => x.ErpnextAccountName)
                .NotEmpty();
        });
    }
}

internal class UpsertCompanyExpenseMappingsCommandHandler(DashboardDbContext db, IUserContext userContext) : ICommandHandler<UpsertCompanyExpenseMappingsCommand>
{
    public async Task Handle(UpsertCompanyExpenseMappingsCommand request, CancellationToken ct)
    {
        if (!userContext.IsAdmin && !userContext.HasCompany(request.CompanyId))
            throw new UnauthorizedAccessException();

        var existing = await db.CompanyExpenseMappings
            .Where(m => m.CompanyId == request.CompanyId)
            .ToListAsync(ct);

        db.CompanyExpenseMappings.RemoveRange(existing);

        var newMappings = request.Mappings.Select(m => new CompanyExpenseMapping
        {
            CompanyId = request.CompanyId,
            ExpenseTypeId = m.ExpenseTypeId,
            ErpnextAccountName = m.ErpnextAccountName,
        }).ToList();

        await db.CompanyExpenseMappings.AddRangeAsync(newMappings, ct);
        await db.SaveChangesAsync(ct);
    }
}
