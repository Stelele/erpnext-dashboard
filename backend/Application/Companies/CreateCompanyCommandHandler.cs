using Application.Abstractions;
using Domain.Companies;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class CreateCompanyCommandHandler(
    DashboardDbContext db
) : ICommandHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var companyExists = await db.Companies
            .AnyAsync(c => c.Name == request.Name && c.SiteId == request.SiteId, cancellationToken);

        if (companyExists)
            throw new DuplicateDomainMemberException("A Company with the same name already exists for this Site.");

        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken)
            ?? throw new KeyNotFoundException($"The Site referenced does not exist: {request.SiteId}");

        var company = Company.Create(site, request.Name, request.Description);

        await db.Companies.AddAsync(company, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}
