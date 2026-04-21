using Domain.Abstractions;
using Domain.Sites;
using Domain.Users;

namespace Domain.Companies;

public class Company : Base
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public required Site Site { get; set; }
    public List<User> Users { get; set; } = [];

    public static Company Create(
        Site site,
        string name,
        string description = "",
        List<User>? users = default)
    {
        return new Company
        {
            SiteId = site.Id,
            Name = name,
            Description = description,
            Users = users ?? [],
            Site = site,
        };
    }
}
