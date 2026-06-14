using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanyExpenseMappings;

public record GetCompanyExpenseMappingsQuery(Guid CompanyId) : IQuery<List<CompanyExpenseMappingResponse>>;

internal class GetCompanyExpenseMappingsQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanyExpenseMappingsQuery, List<CompanyExpenseMappingResponse>>
{
    public async Task<List<CompanyExpenseMappingResponse>> Handle(GetCompanyExpenseMappingsQuery request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            return [];

        return await db.CompanyExpenseMappings
            .Include(m => m.ExpenseType)
            .Where(m => m.CompanyId == request.CompanyId)
            .Select(m => CompanyExpenseMappingResponse.FromDomain(m, m.ExpenseType.Name))
            .ToListAsync(ct);
    }
}
