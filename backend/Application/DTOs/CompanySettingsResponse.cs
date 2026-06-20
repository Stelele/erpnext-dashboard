using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;

namespace Application.DTOs;

public record CompanySettingsResponse(
    Guid Id,
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor,
    NeutralColor? NeutralColor,
    ThemeMode? ThemeMode,
    string? PackSizeMap,
    string? AccountFilters
)
{
    public static CompanySettingsResponse FromDomain(CompanySettingsEntity settings) =>
        new(
            settings.Id,
            settings.CompanyId,
            settings.DefaultIncomeAccountName,
            settings.PrimaryColor,
            settings.NeutralColor,
            settings.ThemeMode,
            settings.PackSizeMap,
            settings.AccountFilters
        );
}
