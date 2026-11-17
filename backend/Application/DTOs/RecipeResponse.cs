namespace Application.DTOs;

public record RecipeResponse(
    Guid Id,
    string Name,
    string Description,
    List<FeatureResponse> Features
);
