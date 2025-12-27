using Application.Abstractions;
using Domain.Exceptions;
using Domain.Sites;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class CreateSiteCommandHandler(
    DashboardDbContext db
) : ICommandHandler<CreateSiteCommand, Guid>
{
    public async Task<Guid> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var siteExists = await db.Sites.AnyAsync(s => s.Url == request.Url, cancellationToken);
        if (siteExists)
            throw new DuplicateDomainMemberException($"A site with the url already exists: {request.Url}");

        var users = await db.Users
            .Where(u => request.Users.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var companies = await db.Companies
            .Where(c => request.Companies.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var site = Site.Create(
            name: request.Name,
            url: request.Url,
            description: request.Description,
            apiToken: request.ApiToken,
            users: users,
            companies: companies
        );

        await db.Sites.AddAsync(site, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return site.Id;
    }
}
