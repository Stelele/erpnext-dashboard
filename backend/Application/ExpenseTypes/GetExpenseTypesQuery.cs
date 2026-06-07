using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record GetExpenseTypesQuery : IQuery<List<ExpenseTypeResponse>>;

internal class GetExpenseTypesQueryHandler(DashboardDbContext db) : IQueryHandler<GetExpenseTypesQuery, List<ExpenseTypeResponse>>
{
    public async Task<List<ExpenseTypeResponse>> Handle(GetExpenseTypesQuery request, CancellationToken ct)
    {
        return await db.ExpenseTypes
            .OrderBy(e => e.Name)
            .Select(e => ExpenseTypeResponse.FromDomain(e))
            .ToListAsync(ct);
    }
}
