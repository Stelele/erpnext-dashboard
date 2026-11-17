using Domain.Abstractions;
using Domain.Companies;

namespace Domain.Features;

public class CompanyRecipe : Base
{
    public Guid CompanyId { get; set; }
    public Guid RecipeId { get; set; }

    public Company Company { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
