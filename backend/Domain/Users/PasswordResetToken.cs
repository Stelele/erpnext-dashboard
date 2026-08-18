using Domain.Abstractions;

namespace Domain.Users;

public class PasswordResetToken : Base
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset? UsedOn { get; set; }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset expiresOn)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresOn = expiresOn
        };
    }

    public bool IsUsable(DateTimeOffset now) => UsedOn is null && now < ExpiresOn;
}
