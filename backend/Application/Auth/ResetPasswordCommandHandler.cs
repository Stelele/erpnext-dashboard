using Application.Abstractions;
using Application.DTOs;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Auth;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth;

public class ResetPasswordCommandHandler(
    DashboardDbContext db,
    IPasswordHasher<User> passwordHasher
) : ICommandHandler<ResetPasswordCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = OpaqueToken.Hash(request.Token);
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Companies)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedException("Invalid or expired reset token.");

        if (!resetToken.IsUsable(DateTimeOffset.UtcNow))
            throw new UnauthorizedException("Invalid or expired reset token.");

        var user = resetToken.User;
        var now = DateTimeOffset.UtcNow;

        // Atomically consume the token so a concurrent request can't replay it.
        var consumed = await db.PasswordResetTokens
            .Where(t => t.Id == resetToken.Id && t.UsedOn == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedOn, now), ct);

        if (consumed == 0)
            throw new UnauthorizedException("Invalid or expired reset token.");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;

        // Invalidate any other outstanding reset tokens for this user.
        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Id != resetToken.Id && t.UsedOn == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedOn, now), ct);

        // Revoke all existing sessions so the old token stops working.
        await db.Sessions.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(ct);

        var token = OpaqueToken.Generate();
        db.Sessions.Add(Session.Create(user.Id, OpaqueToken.Hash(token)));

        await db.SaveChangesAsync(ct);

        return new LoginResponse(token, UserResponse.FromDomain(user));
    }
}
