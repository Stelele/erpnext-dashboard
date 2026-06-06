using Application.Abstractions;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class AddUserToCompanyCommandHandler(DashboardDbContext db) : ICommandHandler<AddUserToCompanyCommand>
{
    public async Task Handle(AddUserToCompanyCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([request.UserId], cancellationToken: cancellationToken) 
            ?? throw new NotFoundException($"User with ID {request.UserId} not found.");

        var company = await db.Companies.FindAsync([request.CompanyId], cancellationToken: cancellationToken) 
            ?? throw new NotFoundException($"Company with ID {request.CompanyId} not found.");
            
        var alreadyLinked = await db.Users
            .AnyAsync(u => u.Id == request.UserId && u.Companies.Any(c => c.Id == request.CompanyId), cancellationToken);

        if (alreadyLinked)
            throw new DuplicateDomainMemberException("User is already linked to this company.");

        user.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);
    }
}
