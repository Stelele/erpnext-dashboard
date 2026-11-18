using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public record GetFeaturesQuery() : IQuery<List<FeatureResponse>>;

public class GetFeaturesQueryHandler(DashboardDbContext db) : IQueryHandler<GetFeaturesQuery, List<FeatureResponse>>
{
    public async Task<List<FeatureResponse>> Handle(GetFeaturesQuery request, CancellationToken ct)
    {
        var features = await db.Features.AsNoTracking().OrderBy(f => f.Code).ToListAsync(ct);

        return features.Select(FeatureResponse.FromDomain).ToList();
    }
}
