using Domain.CompanySettings;
using FluentValidation;

namespace Application.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null,
    List<PackSizeEntry>? PackSizeMap = null
);

public class UpdateCompanySettingsRequestValidator : AbstractValidator<UpdateCompanySettingsRequest>
{
    public UpdateCompanySettingsRequestValidator()
    {
        RuleFor(x => x.DefaultIncomeAccountName).NotEmpty();
    }
}
