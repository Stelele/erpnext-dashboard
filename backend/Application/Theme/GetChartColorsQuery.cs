using Application.Abstractions;
using Application.Caching;
using Application.ChartColors;
using Application.DTOs;
using Domain.CompanySettings;

namespace Application.Theme;

[Cache(DurationMinutes = 1440, KeyPrefix = "chart_colors")]
public record GetChartColorsQuery(PrimaryColor PrimaryColor) : IQuery<ChartColorsResponse?>;

internal class GetChartColorsQueryHandler : IQueryHandler<GetChartColorsQuery, ChartColorsResponse?>
{
    public Task<ChartColorsResponse?> Handle(GetChartColorsQuery request, CancellationToken ct)
    {
        var colors = ChartColorData.GetColors(request.PrimaryColor);
        if (colors is null)
            return Task.FromResult<ChartColorsResponse?>(null);

        return Task.FromResult<ChartColorsResponse?>(new ChartColorsResponse(request.PrimaryColor, colors));
    }
}
