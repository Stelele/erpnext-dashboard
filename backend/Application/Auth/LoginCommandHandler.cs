using Application.Abstractions;
using Application.DTOs;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Auth;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth;

public class LoginCommandHandler(
    DashboardDbContext db,
    IPasswordHasher<User> passwordHasher
) : ICommandHandler<LoginCommand, LoginResponse>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly string DummyPasswordHash = new PasswordHasher<User>().HashPassword(new User(), "dummy-password-value");

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is not null && user.LockoutUntil is { } lockoutUntil && lockoutUntil > DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Invalid email or password.");

        var storedHash = user?.PasswordHash ?? DummyPasswordHash;
        var result = passwordHasher.VerifyHashedPassword(user ?? new User(), storedHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            if (user is not null && user.PasswordHash is not null)
            {
                user.FailedLoginCount += 1;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.FailedLoginCount = 0;
                    user.LockoutUntil = DateTimeOffset.UtcNow + LockoutDuration;
                }
                await db.SaveChangesAsync(ct);
            }
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;

        var token = OpaqueToken.Generate();
        db.Sessions.Add(Session.Create(user.Id, OpaqueToken.Hash(token)));
        await db.SaveChangesAsync(ct);

        return new LoginResponse(token, UserResponse.FromDomain(user));
    }
}
