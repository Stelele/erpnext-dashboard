using Domain.Users;
using Infrastructure.Auth;
using Infrastructure.Models;

namespace Application.Auth;

public interface IPasswordResetTokenService
{
    Task<string> CreateAsync(Guid userId, CancellationToken ct);
}

public class PasswordResetTokenService(DashboardDbContext db) : IPasswordResetTokenService
{
    public async Task<string> CreateAsync(Guid userId, CancellationToken ct)
    {
        var raw = OpaqueToken.Generate();
        db.PasswordResetTokens.Add(PasswordResetToken.Create(
            userId,
            OpaqueToken.Hash(raw),
            DateTimeOffset.UtcNow.AddHours(24)));
        await db.SaveChangesAsync(ct);
        return raw;
    }
}
