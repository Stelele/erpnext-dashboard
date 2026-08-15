using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSiteByIdQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetSiteByIdQuery, SiteResponse?>
{
    public async Task<SiteResponse?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var query = db.Sites.Include(s => s.Companies).Where(s => s.Id == request.Id);

        if (!userContext.IsAdmin)
            query = query.Where(s => s.Companies.Any(c => userContext.CompanyIds.Contains(c.Id)));

        var site = await query.FirstOrDefaultAsync(cancellationToken);
        return site == null ? null : SiteResponse.FromDomain(site);
    }
}
