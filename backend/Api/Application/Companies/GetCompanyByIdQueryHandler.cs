using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompanyByIdQueryHandler(
    DashboardDbContext db
) : IQueryHandler<GetCompanyByIdQuery, ExtendedCompanyResponse?>
{
    public async Task<ExtendedCompanyResponse?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (company == null) return null;
        return ExtendedCompanyResponse.FromDomain(company);
    }
}
