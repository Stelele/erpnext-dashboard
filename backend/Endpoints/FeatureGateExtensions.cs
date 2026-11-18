using Application.Features;
using Infrastructure.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Api;

public static class FeatureGateExtensions
{
    public static RouteHandlerBuilder RequireFeature(this RouteHandlerBuilder builder, string featureCode)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var routeValues = context.HttpContext.Request.RouteValues;
            if (!routeValues.TryGetValue("companyId", out var raw) || raw is null || !Guid.TryParse(raw.ToString(), out var companyId))
            {
                return Results.NotFound();
            }

            using var scope = context.HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
            var codes = await CompanyFeaturesResolver.GetEffectiveCodesAsync(db, companyId, context.HttpContext.RequestAborted);

            if (!codes.Contains(featureCode))
            {
                return Results.NotFound();
            }

            return await next(context);
        });
    }
}
