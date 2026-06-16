using Application.Abstractions;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Auth0;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Users;

public class CreateUserCommandHandler(
    DashboardDbContext db,
    Auth0UserProvisioner provisioner,
    ILogger<CreateUserCommandHandler> logger
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

        try
        {
            await db.Users.AddAsync(user, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception) when (auth0User?.UserId is not null)
        {
            try
            {
                await provisioner.DeleteUserAsync(auth0User.UserId, CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Failed to clean up Auth0 user {Auth0UserId} after database save failure",
                    auth0User.UserId);
            }

            throw;
        }

        return user.Id;
    }
}
