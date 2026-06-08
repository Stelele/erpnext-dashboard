using FluentValidation;

namespace Application.Requests;

public record CreateExpenseTypeRequest(
    string Name,
    string Description
);

public class CreateExpenseTypeRequestValidator : AbstractValidator<CreateExpenseTypeRequest>
{
    public CreateExpenseTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}
