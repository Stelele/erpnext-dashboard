using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSitesQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetSitesQuery, List<SiteResponse>>
{
    public async Task<List<SiteResponse>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Sites.Include(s => s.Companies).AsQueryable();

        if (!userContext.IsAdmin)
            query = query.Where(s => s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id)));

        if (request.Sites != null && request.Sites.Length != 0)
            query = query.Where(s => request.Sites.Contains(s.Id));

        return [.. query.Select(SiteResponse.FromDomain)];
    }
}
