using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public static class CompanyFeaturesResolver
{
    public static async Task<List<string>> GetEffectiveCodesAsync(DashboardDbContext db, Guid companyId, CancellationToken ct)
    {
        var result = await GetEffectiveCodesAsync(db, new[] { companyId }, ct);
        return result.TryGetValue(companyId, out var codes) ? codes : [];
    }

    public static async Task<Dictionary<Guid, List<string>>> GetEffectiveCodesAsync(DashboardDbContext db, IReadOnlyCollection<Guid> companyIds, CancellationToken ct)
    {
        if (companyIds.Count == 0)
            return new();

        var rows = await (
            from cr in db.CompanyRecipes
            join r in db.Recipes on cr.RecipeId equals r.Id
            join rf in db.RecipeFeatures on r.Id equals rf.RecipeId
            join f in db.Features on rf.FeatureId equals f.Id
            where companyIds.Contains(cr.CompanyId)
            select new { cr.CompanyId, f.Code })
            .Distinct()
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.CompanyId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.Code).ToList()
            );
    }
}
