using Application.Abstractions;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName
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
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
        }

        await db.SaveChangesAsync(ct);
    }
}
