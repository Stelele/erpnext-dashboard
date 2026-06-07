using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;

namespace Application.DTOs;

public record CompanySettingsResponse(Guid Id, Guid CompanyId, string DefaultIncomeAccountName)
{
    public static CompanySettingsResponse FromDomain(CompanySettingsEntity settings) =>
        new(settings.Id, settings.CompanyId, settings.DefaultIncomeAccountName);
}
