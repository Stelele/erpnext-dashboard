using Application.Abstractions;
using Application.Caching;
using FluentValidation;

namespace Application.Users;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
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
