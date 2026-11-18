using Application.Abstractions;
using Infrastructure.Models;

namespace Application.Features;

public record GetCompanyFeaturesQuery(Guid CompanyId) : IQuery<List<string>>;

public class GetCompanyFeaturesQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanyFeaturesQuery, List<string>>
{
    public Task<List<string>> Handle(GetCompanyFeaturesQuery request, CancellationToken ct)
        => CompanyFeaturesResolver.GetEffectiveCodesAsync(db, request.CompanyId, ct);
}
