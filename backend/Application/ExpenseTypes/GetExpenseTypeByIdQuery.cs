using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record GetExpenseTypeByIdQuery(Guid Id) : IQuery<ExpenseTypeResponse?>;

public class GetExpenseTypeByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetExpenseTypeByIdQuery, ExpenseTypeResponse?>
{
    public async Task<ExpenseTypeResponse?> Handle(GetExpenseTypeByIdQuery request, CancellationToken ct)
    {
        var expenseType = await db.ExpenseTypes
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

        return expenseType == null ? null : ExpenseTypeResponse.FromDomain(expenseType);
    }
}
