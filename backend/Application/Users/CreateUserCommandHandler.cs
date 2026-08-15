using Application.Abstractions;
using Application.Auth;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Email;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Users;

public class CreateUserCommandHandler(
    DashboardDbContext db,
    IPasswordResetTokenService resetTokenService,
    IEmailSender emailSender,
    IConfiguration configuration,
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

        var user = User.Create(request.Name, request.Email, request.Role, companies);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var token = await resetTokenService.CreateAsync(user.Id, cancellationToken);
            var frontendUrl = configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl is null");
            var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
            await emailSender.SendAsync(EmailTemplates.PasswordSetup(user.Email, user.Name, resetUrl), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password setup email for user {Email}", user.Email);
        }

        return user.Id;
    }
}
