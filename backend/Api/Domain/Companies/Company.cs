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
}
