namespace Application.Users;

public interface IUserContext
{
    Guid UserId { get; }
    IReadOnlyList<Guid> CompanyIds { get; }
    bool IsAdmin { get; }
    bool HasCompany(Guid companyId);
}
