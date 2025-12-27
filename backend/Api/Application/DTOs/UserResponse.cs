using Domain.Users;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public class UserResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("sites")]
    public List<Guid> Sites { get; set; } = [];

    [JsonPropertyName("companies")]
    public List<Guid> Companies { get; set; } = [];

    public static UserResponse FromDomain(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Sites = user.Sites.Select(site => site.Id).ToList(),
            Companies = user.Companies.Select(company => company.Id).ToList()
        };
    }
}
