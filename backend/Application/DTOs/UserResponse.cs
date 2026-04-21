using Domain.Companies;
using Domain.Users;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public class BaseUserResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}

public class UserResponse : BaseUserResponse
{
    [JsonPropertyName("companies")]
    public List<Guid> Companies { get; init; } = [];

    public static UserResponse FromDomain(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Companies = [.. user.Companies.Select(company => company.Id)]
        };
    }
}

public class SiteCompanyResponse : BaseCompanyResponse
{
    public required BaseSiteResponse Site { get; init; }
    public static SiteCompanyResponse FromDomain(Company company)
    {
        return new SiteCompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            Site = new BaseSiteResponse
            {
                Id = company.Site.Id,
                Name = company.Site.Name,
                Description = company.Site.Description,
                Url = company.Site.Url,
                ApiToken = company.Site.ApiToken
            }
        };
    }
}

public class ExtendedUserResponse : BaseUserResponse
{
    [JsonPropertyName("companies")]
    public List<SiteCompanyResponse> Companies { get; init; } = [];

    public static ExtendedUserResponse FromDomain(User user)
    {
        return new ExtendedUserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Companies = [.. user.Companies.Select(SiteCompanyResponse.FromDomain)]
        };
    }
}
