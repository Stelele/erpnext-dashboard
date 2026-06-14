using Application.Abstractions;
using Application.DTOs;
using Application.Users;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

public record GetCompanySettingsQuery(Guid CompanyId) : IQuery<CompanySettingsResponse?>;

internal class GetCompanySettingsQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?>
{
    public async Task<CompanySettingsResponse?> Handle(GetCompanySettingsQuery request, CancellationToken ct)
    {
        if (userContext.CompanyIds.Count > 0 && !userContext.CompanyIds.Contains(request.CompanyId))
            return null;

        var settings = await db.CompanySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        return settings == null ? null : CompanySettingsResponse.FromDomain(settings);
    }
}
