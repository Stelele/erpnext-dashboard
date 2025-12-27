using Application.Abstractions;
using MediatR;

namespace Application.Users;

public record DeleteUserCommand(Guid Id) : ICommand<Unit>;
