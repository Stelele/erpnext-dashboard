using Application.Abstractions;
using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null
) : ICommand;

internal class UpdateCompanySettingsCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateCompanySettingsCommand>
{
    public async Task Handle(UpdateCompanySettingsCommand request, CancellationToken ct)
    {
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
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
            settings.PrimaryColor = request.PrimaryColor;
            settings.NeutralColor = request.NeutralColor;
            settings.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
