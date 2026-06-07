using Application.Abstractions;
using Domain.CompanyExpenseMappings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanyExpenseMappings;

public record UpsertCompanyExpenseMappingsCommand(
    Guid CompanyId,
    List<MappingItem> Mappings
) : ICommand;

public record MappingItem(Guid ExpenseTypeId, string ErpnextAccountName);

internal class UpsertCompanyExpenseMappingsCommandHandler(DashboardDbContext db) : ICommandHandler<UpsertCompanyExpenseMappingsCommand>
{
    public async Task Handle(UpsertCompanyExpenseMappingsCommand request, CancellationToken ct)
    {
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
