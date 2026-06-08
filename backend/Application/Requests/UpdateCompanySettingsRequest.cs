using FluentValidation;

namespace Application.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName
);

public class UpdateCompanySettingsRequestValidator : AbstractValidator<UpdateCompanySettingsRequest>
{
    public UpdateCompanySettingsRequestValidator()
    {
        RuleFor(x => x.DefaultIncomeAccountName).NotEmpty();
    }
}
