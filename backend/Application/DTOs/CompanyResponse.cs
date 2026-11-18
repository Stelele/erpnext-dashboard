using Domain.Companies;

namespace Application.DTOs;

public record CompanyResponse(
    Guid Id,
    string Name,
    string Description,
    Guid SiteId,
    string[] Features
)
{
    public static CompanyResponse FromDomain(Company company, IEnumerable<string> features) =>
        new(
            company.Id,
            company.Name,
            company.Description,
            company.SiteId,
            features.ToArray()
        );
}
