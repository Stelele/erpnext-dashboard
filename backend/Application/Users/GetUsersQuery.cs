using Application.Abstractions;
using Application.Caching;
using Application.DTOs;

namespace Application.Users;

[Cache(DurationMinutes = 5, KeyPrefix = "users")]
public record GetUsersQuery(
    Guid[]? UserIds = null
) : IQuery<List<UserResponse>>;
