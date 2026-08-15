using Application.Abstractions;
using Application.DTOs;
using FluentValidation;

namespace Application.Auth;

public record LoginResponse(string Token, UserResponse User);

public record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
