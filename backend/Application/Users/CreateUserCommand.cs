using Application.Abstractions;
using Domain.Users;
using FluentValidation;

namespace Application.Users;

public record CreateUserCommand(
    string Name,
    string Email,
    Role Role,
    List<Guid> Companies
) : ICommand<Guid>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}

