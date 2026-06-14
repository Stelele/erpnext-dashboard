using Application.Abstractions;
using Application.Caching;
using FluentValidation;

namespace Application.Users;

[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "company")]
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
