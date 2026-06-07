using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

public record GetCompanySettingsQuery(Guid CompanyId) : IQuery<CompanySettingsResponse?>;

internal class GetCompanySettingsQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?>
{
    public async Task<CompanySettingsResponse?> Handle(GetCompanySettingsQuery request, CancellationToken ct)
    {
        var settings = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        return settings == null ? null : CompanySettingsResponse.FromDomain(settings);
    }
}
