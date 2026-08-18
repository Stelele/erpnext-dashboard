using Domain.Users;

namespace Application.DTOs;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    Role Role,
    List<Guid> Companies
)
{
    public static UserResponse FromDomain(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            [.. user.Companies.Select(c => c.Id)]
        );
}
