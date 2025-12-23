namespace Domain;

public class Company : Base
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Site Site { get; set; }
    public List<User> Users { get; set; } = [];
}
