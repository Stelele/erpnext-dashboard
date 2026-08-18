using Application.Abstractions;
using Infrastructure.Email;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Auth;

public class ForgotPasswordCommandHandler(
    DashboardDbContext db,
    IPasswordResetTokenService resetTokenService,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<ForgotPasswordCommandHandler> logger
) : ICommandHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // Always return success; don't reveal whether the email exists.
        if (user is null)
            return Unit.Value;

        try
        {
            var token = await resetTokenService.CreateAsync(user.Id, ct);
            var frontendUrl = configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl is null");
            var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
            await emailSender.SendAsync(EmailTemplates.PasswordReset(user.Email, user.Name, resetUrl), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password reset email for {Email}", user.Email);
        }

        return Unit.Value;
    }
}
