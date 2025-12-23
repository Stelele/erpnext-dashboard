using Application.Abstractions;
using FluentValidation;

namespace Application.Users;

public record CreateUserCommand(
    string Name,
    string Email,
    List<Guid> Sites,
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
    }
}

