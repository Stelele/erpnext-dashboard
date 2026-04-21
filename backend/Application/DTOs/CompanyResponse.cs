using System.Text.Json.Serialization;

namespace Application.DTOs;

public class BaseCompanyResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}

public class CompanyResponse : BaseCompanyResponse
{
    [JsonPropertyName("siteId")]
    public Guid SiteId { get; init; }

    public static CompanyResponse FromDomain(Domain.Companies.Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            SiteId = company.SiteId,
            Name = company.Name,
            Description = company.Description
        };
    }
}

public class ExtendedCompanyResponse : BaseCompanyResponse
{
    [JsonPropertyName("site")]
    public required SiteResponse Site { get; init; }

    public static ExtendedCompanyResponse FromDomain(Domain.Companies.Company company)
    {
        return new ExtendedCompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            Site = SiteResponse.FromDomain(company.Site)
        };
    }
}
