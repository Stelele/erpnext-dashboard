using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompanyByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanyByIdQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        return company == null ? null : CompanyResponse.FromDomain(company);
    }
}
