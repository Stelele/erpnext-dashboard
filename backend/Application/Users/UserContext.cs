namespace Application.Users;

public class UserContext : IUserContext
{
    public Guid UserId { get; set; }
    public IReadOnlyList<Guid> CompanyIds { get; set; } = Array.Empty<Guid>();
    public bool IsAdmin { get; set; }
    public bool HasCompany(Guid companyId) => CompanyIds.Contains(companyId);
}
