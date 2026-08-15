using Domain.CompanySettings;

namespace Application.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null,
    List<PackSizeEntry>? PackSizeMap = null
);
