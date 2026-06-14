using Application.Abstractions;
using Application.Caching;
using Application.Users;
using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

[InvalidateCache(Category = "settings")]
public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null
) : ICommand;

internal class UpdateCompanySettingsCommandHandler(DashboardDbContext db, IUserContext userContext) : ICommandHandler<UpdateCompanySettingsCommand>
{
    public async Task Handle(UpdateCompanySettingsCommand request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            throw new UnauthorizedAccessException();

        var settings = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        if (settings == null)
        {
            settings = new CompanySettingsEntity
            {
                CompanyId = request.CompanyId,
                DefaultIncomeAccountName = request.DefaultIncomeAccountName,
                PrimaryColor = request.PrimaryColor,
                NeutralColor = request.NeutralColor,
                ThemeMode = request.ThemeMode,
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
            settings.PrimaryColor = request.PrimaryColor;
            settings.NeutralColor = request.NeutralColor;
            settings.ThemeMode = request.ThemeMode;
            settings.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
