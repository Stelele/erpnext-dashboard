namespace Application.Requests;

public record CreateFeatureRequest(
    string Code,
    string Name,
    string Description
);

public record UpdateFeatureRequest(
    string Name,
    string Description
);
