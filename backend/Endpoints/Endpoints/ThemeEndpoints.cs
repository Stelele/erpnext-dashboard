using Application.ChartColors;
using Application.DTOs;
using Domain.CompanySettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class ThemeEndpoints
{
    public static WebApplication MapThemeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/theme/chart-colors", (string? primaryColor) =>
            {
                if (primaryColor == null || !Enum.TryParse<PrimaryColor>(primaryColor, true, out var parsed))
                {
                    return Results.BadRequest($"Invalid primary color: {primaryColor}");
                }

                var colors = ChartColorData.GetColors(parsed);
                if (colors == null)
                {
                    return Results.BadRequest($"No palette for: {primaryColor}");
                }

                return Results.Ok(new ChartColorsResponse(parsed, colors));
            })
            .WithTags(Tags.Companies)
            .WithName("GetChartColors")
            .Produces<ChartColorsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequireAuthorization(Permissions.ReadCompanies);

        return app;
    }
}
