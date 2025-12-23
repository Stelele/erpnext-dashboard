namespace Domain;

public class Site : Base
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;

    public List<User> Users { get; set; } = [];
    public List<Company> Companies { get; set; } = [];
}
