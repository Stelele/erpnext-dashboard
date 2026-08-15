using Application.Abstractions;
using Application.DTOs;
using FluentValidation;

namespace Application.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommand<LoginResponse>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}
