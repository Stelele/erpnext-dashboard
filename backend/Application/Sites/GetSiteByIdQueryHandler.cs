using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSiteByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetSiteByIdQuery, SiteResponse?>
{
    public async Task<SiteResponse?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites
            .Include(s => s.Companies)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        return site == null ? null : SiteResponse.FromDomain(site);
    }
}
