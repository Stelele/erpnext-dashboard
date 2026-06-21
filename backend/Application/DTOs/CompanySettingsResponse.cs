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
    List<PackSizeEntry>? PackSizeMap
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
            settings.PackSizeMap
        );
}
