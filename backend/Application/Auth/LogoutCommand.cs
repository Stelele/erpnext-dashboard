using Application.Abstractions;
using MediatR;

namespace Application.Auth;

public record LogoutCommand(string Token) : ICommand<Unit>;
