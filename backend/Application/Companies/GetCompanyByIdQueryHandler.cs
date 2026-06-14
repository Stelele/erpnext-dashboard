using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompanyByIdQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanyByIdQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var query = db.Companies.AsNoTracking().Where(c => c.Id == request.Id);

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(c => userContext.CompanyIds.Contains(c.Id));

        var company = await query.FirstOrDefaultAsync(cancellationToken);
        return company == null ? null : CompanyResponse.FromDomain(company);
    }
}
