using FluentValidation;

namespace Application.Requests;

public record UpdateExpenseTypeRequest(
    string Name,
    string Description
);

public class UpdateExpenseTypeRequestValidator : AbstractValidator<UpdateExpenseTypeRequest>
{
    public UpdateExpenseTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}
