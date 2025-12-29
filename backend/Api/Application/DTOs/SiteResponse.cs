using Domain.Sites;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public class BaseSiteResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("apiToken")]
    public string ApiToken { get; init; } = string.Empty;
}

public class SiteResponse : BaseSiteResponse
{
    [JsonPropertyName("companies")]
    public List<Guid> Companies { get; init; } = [];

    public static SiteResponse FromDomain(Site site)
    {
        return new SiteResponse
        {
            Id = site.Id,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            ApiToken = site.ApiToken,
            Companies = [.. site.Companies.Select(company => company.Id)]
        };
    }
}

public class ExtendedSiteResponse : BaseSiteResponse
{
    [JsonPropertyName("companies")]
    public List<CompanyResponse> Companies { get; init; } = [];

    public static ExtendedSiteResponse FromDomain(Site site)
    {
        return new ExtendedSiteResponse
        {
            Id = site.Id,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            ApiToken = site.ApiToken,
            Companies = [.. site.Companies.Select(CompanyResponse.FromDomain)]
        };
    }
}
