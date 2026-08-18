using Domain.Users;

namespace Application.Requests;

public record CreateUserRequest(
    string Name,
    string Email,
    Role Role,
    List<Guid> Companies
);
