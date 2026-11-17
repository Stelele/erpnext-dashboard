using Domain.Abstractions;

namespace Domain.Features;

public class Recipe : Base
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
