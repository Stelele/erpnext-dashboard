using Application.Abstractions;
using Application.DTOs;

namespace Application.Users;

public record GetUsersQuery(
    Guid[]? UserIds = null
) : IQuery<List<UserResponse>>;
