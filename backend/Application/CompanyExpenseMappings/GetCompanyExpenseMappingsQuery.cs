using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanyExpenseMappings;

public record GetCompanyExpenseMappingsQuery(Guid CompanyId) : IQuery<List<CompanyExpenseMappingResponse>>;

internal class GetCompanyExpenseMappingsQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanyExpenseMappingsQuery, List<CompanyExpenseMappingResponse>>
{
    public async Task<List<CompanyExpenseMappingResponse>> Handle(GetCompanyExpenseMappingsQuery request, CancellationToken ct)
    {
        return await db.CompanyExpenseMappings
            .Include(m => m.ExpenseType)
            .Where(m => m.CompanyId == request.CompanyId)
            .Select(m => CompanyExpenseMappingResponse.FromDomain(m, m.ExpenseType.Name))
            .ToListAsync(ct);
    }
}
