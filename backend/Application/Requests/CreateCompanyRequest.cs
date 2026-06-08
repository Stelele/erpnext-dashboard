using FluentValidation;

namespace Application.Requests;

public record CreateCompanyRequest(
    Guid SiteId,
    string Name,
    string Description
);

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
