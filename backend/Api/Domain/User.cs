namespace Domain;

public class User : Base
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<Site> Sites { get; set; } = [];
    public List<Company> Companies { get; set; } = [];

    public static User Create(string name, string email, List<Site> sites, List<Company> companies)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Sites = sites,
            Companies = companies,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };
    }
}
