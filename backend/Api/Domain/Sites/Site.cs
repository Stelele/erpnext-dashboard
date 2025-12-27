using Domain.Abstractions;
using Domain.Companies;
using Domain.Users;

namespace Domain.Sites;

public class Site : Base
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;

    public List<User> Users { get; set; } = [];
    public List<Company> Companies { get; set; } = [];

    public static Site Create(
        string name,
        string url,
        string description = "",
        string apiToken = "",
        List<User>? users = default,
        List<Company>? companies = default)
    {
        return new Site
        {
            Name = name,
            Url = url,
            Description = description,
            ApiToken = apiToken,
            Users = users ?? [],
            Companies = companies ?? []
        };
    }
}
