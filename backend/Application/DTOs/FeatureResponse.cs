using Domain.Features;

namespace Application.DTOs;

public record FeatureResponse(
    Guid Id,
    string Code,
    string Name,
    string Description
)
{
    public static FeatureResponse FromDomain(Feature feature) =>
        new(
            feature.Id,
            feature.Code,
            feature.Name,
            feature.Description
        );
}
