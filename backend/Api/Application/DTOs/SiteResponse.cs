using Domain.Sites;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public class SiteResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("apiToken")]
    public string ApiToken { get; set; } = string.Empty;

    [JsonPropertyName("users")]
    public List<Guid> Users { get; set; } = [];

    [JsonPropertyName("companies")]
    public List<Guid> Companies { get; set; } = [];

    public static SiteResponse FromDomain(Site site)
    {
        return new SiteResponse
        {
            Id = site.Id,
            Name = site.Name,
            Description = site.Description,
            Url = site.Url,
            ApiToken = site.ApiToken,
            Users = [.. site.Users.Select(user => user.Id)],
            Companies = [.. site.Companies.Select(company => company.Id)]
        };
    }
}
