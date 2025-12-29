using Application.Abstractions;
using Application.DTOs;

namespace Application.Users;

public record GetUserByIdQuery(
    Guid Id
) : IQuery<ExtendedUserResponse?>;
