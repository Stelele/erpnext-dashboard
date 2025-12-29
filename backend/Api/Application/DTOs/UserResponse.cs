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

public class ExtendedUserResponse : BaseUserResponse
{
    [JsonPropertyName("companies")]
    public List<CompanyResponse> Companies { get; init; } = [];

    public static ExtendedUserResponse FromDomain(User user)
    {
        return new ExtendedUserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Companies = [.. user.Companies.Select(CompanyResponse.FromDomain)]
        };
    }
}
