using Application.Abstractions;
using Application.Caching;
using Domain.Users;
using FluentValidation;
using MediatR;

namespace Application.Users;

[InvalidateCache(Category = "users")]
public record UpdateUserRoleCommand(Guid UserId, Role Role) : ICommand<Unit>;

public sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
