using Domain.Companies;

namespace Application.DTOs;

public record CompanyResponse(
    Guid Id,
    string Name,
    string Description,
    Guid SiteId
)
{
    public static CompanyResponse FromDomain(Company company) =>
        new(
            company.Id,
            company.Name,
            company.Description,
            company.SiteId
        );
}
