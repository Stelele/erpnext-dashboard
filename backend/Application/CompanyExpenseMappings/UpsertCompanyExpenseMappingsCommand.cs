using Application.Abstractions;
using Application.Users;
using Domain.CompanyExpenseMappings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanyExpenseMappings;

public record UpsertCompanyExpenseMappingsCommand(
    Guid CompanyId,
    List<MappingItem> Mappings
) : ICommand;

public record MappingItem(Guid ExpenseTypeId, string ErpnextAccountName);

internal class UpsertCompanyExpenseMappingsCommandHandler(DashboardDbContext db, IUserContext userContext) : ICommandHandler<UpsertCompanyExpenseMappingsCommand>
{
    public async Task Handle(UpsertCompanyExpenseMappingsCommand request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
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
