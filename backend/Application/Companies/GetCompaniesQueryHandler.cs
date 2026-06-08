using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompaniesQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompaniesQuery, List<CompanyResponse>>
{
    public async Task<List<CompanyResponse>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = request.CompanyIds != null && request.CompanyIds.Length > 0
            ? db.Companies.Where(c => request.CompanyIds.Contains(c.Id))
            : db.Companies;

        var companies = await query.ToListAsync(cancellationToken);
        return [.. companies.Select(CompanyResponse.FromDomain)];
    }
}
