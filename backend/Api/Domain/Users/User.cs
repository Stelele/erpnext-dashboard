using Domain.Abstractions;
using Domain.Companies;
using Domain.Sites;

namespace Domain.Users;

public class User : Base
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Auth0UserId { get; set; } = string.Empty;

    public List<Site> Sites { get; set; } = [];
    public List<Company> Companies { get; set; } = [];

    public static User Create(
        string name,
        string email,
        List<Site>? sites = default,
        List<Company>? companies = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Sites = sites ?? [],
            Companies = companies ?? []
        };

        return user;
    }
}
