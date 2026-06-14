using Application.Abstractions;
using FluentValidation;

namespace Application.Users;

public record AddUserToCompanyCommand(
    Guid UserId,
    Guid CompanyId
) : ICommand;

public sealed class AddUserToCompanyCommandValidator : AbstractValidator<AddUserToCompanyCommand>
{
    public AddUserToCompanyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}
