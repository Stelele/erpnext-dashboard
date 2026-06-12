using Domain.CompanySettings;
using FluentValidation;

namespace Application.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null
);

public class UpdateCompanySettingsRequestValidator : AbstractValidator<UpdateCompanySettingsRequest>
{
    public UpdateCompanySettingsRequestValidator()
    {
        RuleFor(x => x.DefaultIncomeAccountName).NotEmpty();
    }
}
