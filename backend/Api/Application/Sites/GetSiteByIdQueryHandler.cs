using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;

namespace Application.Sites;

public class GetSiteByIdQueryHandler(
    DashboardDbContext db
) : IQueryHandler<GetSiteByIdQuery, SiteResponse?>
{
    public async Task<SiteResponse?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.FindAsync([request.Id], cancellationToken);
        if (site == null) return null;
        return SiteResponse.FromDomain(site);
    }
}
