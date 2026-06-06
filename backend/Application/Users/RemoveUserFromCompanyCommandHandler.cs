using Application.Abstractions;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class RemoveUserFromCompanyCommandHandler(DashboardDbContext db) : ICommandHandler<RemoveUserFromCompanyCommand>
{
    public async Task Handle(RemoveUserFromCompanyCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException($"User with ID {request.UserId} not found.");

        var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId) 
            ?? throw new NotFoundException($"Link between user and company {request.CompanyId} not found.");
            
        user.Companies.Remove(company);
        await db.SaveChangesAsync(cancellationToken);
    }
}
