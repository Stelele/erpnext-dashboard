using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public interface IGoogleTokenProvider
{
    string? CurrentToken { get; }
    bool HasToken { get; }
    void SetToken(string token);
    Task LoadFromConfigurationAsync(IConfiguration configuration, string contentRootPath);
    Task SaveToConfigurationAsync(IConfiguration configuration, string contentRootPath);
}