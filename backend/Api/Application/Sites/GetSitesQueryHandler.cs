using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSitesQueryHandler(
    DashboardDbContext db
) : IQueryHandler<GetSitesQuery, List<SiteResponse>>
{
    public async Task<List<SiteResponse>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var sites = request.Sites != null && request.Sites.Length != 0
            ? await db.Sites.Where(s => request.Sites.Contains(s.Id)).ToListAsync(cancellationToken)
            : await db.Sites.ToListAsync(cancellationToken);

        return [.. sites.Select(SiteResponse.FromDomain)];
    }
}
