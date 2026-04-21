using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSiteByIdQueryHandler(
    DashboardDbContext db
) : IQueryHandler<GetSiteByIdQuery, ExtendedSiteResponse?>
{
    public async Task<ExtendedSiteResponse?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites
            .Include(s => s.Companies)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (site == null) return null;

        return ExtendedSiteResponse.FromDomain(site);
    }
}
