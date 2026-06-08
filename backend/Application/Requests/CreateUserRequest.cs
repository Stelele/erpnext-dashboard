using FluentValidation;

namespace Application.Requests;

public record CreateUserRequest(
    string Name,
    string Email,
    List<Guid> Companies
);

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
