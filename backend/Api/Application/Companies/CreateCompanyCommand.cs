using Application.Abstractions;
using FluentValidation;

namespace Application.Companies;

public record CreateCompanyCommand(
    Guid SiteId,
    string Name,
    string Description
) : ICommand<Guid>;

public class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.SiteId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
