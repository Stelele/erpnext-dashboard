namespace Application.Requests;

public record CreateRecipeRequest(
    string Name,
    string Description,
    Guid[] FeatureIds
);

public record UpdateRecipeRequest(
    string Name,
    string Description,
    Guid[] FeatureIds
);

public record SetCompanyRecipesRequest(Guid[] RecipeIds);
