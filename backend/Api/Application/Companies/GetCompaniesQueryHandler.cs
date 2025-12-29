using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompaniesQueryHandler(
    DashboardDbContext db
) : IQueryHandler<GetCompaniesQuery, List<CompanyResponse>>
{
    public async Task<List<CompanyResponse>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = request.CompanyIds != null && request.CompanyIds.Length > 0
            ? await db.Companies.Where(c => request.CompanyIds.Contains(c.Id)).ToListAsync(cancellationToken)
            : await db.Companies.ToListAsync(cancellationToken);

        return [.. companies.Select(CompanyResponse.FromDomain)];
    }
}
