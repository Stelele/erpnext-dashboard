using Domain.Abstractions;
using Domain.Companies;

namespace Domain.Sites;

public class Site : Base
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;

    public List<Company> Companies { get; set; } = [];

    public static Site Create(
        string name,
        string url,
        string description = "",
        string apiToken = "",
        List<Company>? companies = default)
    {
        return new Site
        {
            Name = name,
            Url = url,
            Description = description,
            ApiToken = apiToken,
            Companies = companies ?? []
        };
    }
}
