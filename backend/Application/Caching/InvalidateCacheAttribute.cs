namespace Application.Caching;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class InvalidateCacheAttribute : Attribute
{
    public string Category { get; init; } = string.Empty;
}
