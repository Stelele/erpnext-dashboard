using Application.DTOs;
using Application.Theme;
using Domain.CompanySettings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class ThemeEndpoints
{
    public static WebApplication MapThemeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/theme/chart-colors", async (string? primaryColor, ISender mediator) =>
            {
                if (primaryColor == null || !Enum.TryParse<PrimaryColor>(primaryColor, true, out var parsed))
                {
                    return Results.BadRequest($"Invalid primary color: {primaryColor}");
                }

                var colors = await mediator.Send(new GetChartColorsQuery(parsed));
                if (colors is null)
                {
                    return Results.BadRequest($"No palette for: {primaryColor}");
                }

                return Results.Ok(colors);
            })
            .WithTags(Tags.Companies)
            .WithName("GetChartColors")
            .Produces<ChartColorsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequireAuthorization(Permissions.ReadCompanies);

        return app;
    }
}
