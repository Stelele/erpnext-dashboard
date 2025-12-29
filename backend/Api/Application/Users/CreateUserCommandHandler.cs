using Application.Abstractions;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Auth0;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class CreateUserCommandHandler(
    DashboardDbContext db,
    Auth0UserProvisioner provisioner
) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userExists = await db.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (userExists)
            throw new DuplicateDomainMemberException($"A user with email {request.Email} already exists.");

        var companies = await db.Companies
            .Where(c => request.Companies.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var user = User.Create(request.Name, request.Email, companies);

        var auth0User = await provisioner.CreateUserInConnectionAsync(
            "Email-Password",
            user.Email,
            user.Id,
            user.Name,
            false,
            cancellationToken);

        user.Auth0UserId = auth0User.UserId;

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
