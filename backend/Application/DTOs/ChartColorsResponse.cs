using Domain.CompanySettings;

namespace Application.DTOs;

public record ChartColorsResponse(
    PrimaryColor PrimaryColor,
    string[] ChartColors
);
