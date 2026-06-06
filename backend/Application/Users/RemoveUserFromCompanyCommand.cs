using Application.Abstractions;
using FluentValidation;

namespace Application.Users;

public record RemoveUserFromCompanyCommand(
    Guid UserId,
    Guid CompanyId
) : ICommand;

public sealed class RemoveUserFromCompanyCommandValidator : AbstractValidator<RemoveUserFromCompanyCommand>
{
    public RemoveUserFromCompanyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}
