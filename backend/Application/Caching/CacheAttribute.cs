namespace Application.Caching;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CacheAttribute : Attribute
{
    public int DurationMinutes { get; init; } = 5;
    public string KeyPrefix { get; init; } = string.Empty;
}
