using Domain.Abstractions;

namespace Domain.Users;

public class Session : Base
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset LastUsedOn { get; set; }

    public static Session Create(Guid userId, string tokenHash)
    {
        return new Session
        {
            UserId = userId,
            TokenHash = tokenHash,
            LastUsedOn = DateTimeOffset.UtcNow
        };
    }
}
