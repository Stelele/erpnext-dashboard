using Domain.Abstractions;
using Domain.Companies;

namespace Domain.Users;

public class User : Base
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Viewer;
    public string? PasswordHash { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }

    public List<Company> Companies { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];
    public List<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    public static User Create(
        string name,
        string email,
        Role role = Role.Viewer,
        List<Company>? companies = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Role = role,
            Companies = companies ?? []
        };

        return user;
    }
}
