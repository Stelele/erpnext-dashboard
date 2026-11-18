using Application.Abstractions;
using Application.DTOs;
using Application.Features;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompaniesQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompaniesQuery, List<CompanyResponse>>
{
    public async Task<List<CompanyResponse>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Companies.AsQueryable();

        if (!userContext.IsAdmin)
            query = query.Where(c => userContext.CompanyIds.Contains(c.Id));

        if (request.CompanyIds != null && request.CompanyIds.Length > 0)
            query = query.Where(c => request.CompanyIds.Contains(c.Id));

        var companies = await query.ToListAsync(cancellationToken);
        var codes = await CompanyFeaturesResolver.GetEffectiveCodesAsync(db, companies.Select(c => c.Id).ToList(), cancellationToken);
        return [.. companies.Select(c => CompanyResponse.FromDomain(c, codes.TryGetValue(c.Id, out var list) ? list : []))];
    }
}
