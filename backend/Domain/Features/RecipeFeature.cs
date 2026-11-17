using Domain.Abstractions;

namespace Domain.Features;

public class RecipeFeature : Base
{
    public Guid RecipeId { get; set; }
    public Guid FeatureId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Feature Feature { get; set; } = null!;
}
