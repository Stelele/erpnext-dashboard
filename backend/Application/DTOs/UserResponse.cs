using Domain.Users;

namespace Application.DTOs;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    List<Guid> Companies
)
{
    public static UserResponse FromDomain(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            [.. user.Companies.Select(c => c.Id)]
        );
}
