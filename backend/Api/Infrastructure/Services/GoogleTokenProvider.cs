using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class GoogleTokenProvider : IGoogleTokenProvider
{
    private readonly ILogger<GoogleTokenProvider> _logger;
    private readonly object _lock = new();
    private string? _currentToken;
    private string? _configPath;

    public string? CurrentToken
    {
        get
        {
            lock (_lock)
            {
                return _currentToken;
            }
        }
    }

    public bool HasToken
    {
        get
        {
            lock (_lock)
            {
                return !string.IsNullOrEmpty(_currentToken);
            }
        }
    }

    public GoogleTokenProvider(ILogger<GoogleTokenProvider> logger)
    {
        _logger = logger;
    }

    public void SetToken(string token)
    {
        lock (_lock)
        {
            _currentToken = token;
        }
        _logger.LogInformation("Token set in memory provider");
    }

    public async Task LoadFromConfigurationAsync(IConfiguration configuration, string contentRootPath)
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        _configPath = Path.Combine(contentRootPath, $"appsettings.{env}.json");

        if (!File.Exists(_configPath))
            _configPath = Path.Combine(contentRootPath, "appsettings.json");

        var token = configuration["GoogleDrive:RefreshToken"];

        lock (_lock)
        {
            _currentToken = token;
        }

        _logger.LogInformation("Token loaded from configuration: {HasToken}", !string.IsNullOrEmpty(token) ? "yes" : "no");
        await Task.CompletedTask;
    }

    public async Task SaveToConfigurationAsync(IConfiguration configuration, string contentRootPath)
    {
        string tokenToSave;
        lock (_lock)
        {
            tokenToSave = _currentToken ?? throw new InvalidOperationException("No token to save");
        }

        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        _configPath = Path.Combine(contentRootPath, $"appsettings.{env}.json");

        if (!File.Exists(_configPath))
            _configPath = Path.Combine(contentRootPath, "appsettings.json");

        _logger.LogInformation("Saving refresh token to {ConfigPath}", _configPath);

        if (string.IsNullOrEmpty(_configPath))
        {
            throw new InvalidOperationException("Config path not initialized");
        }

        string configContent;
        JsonDocument config;

        if (File.Exists(_configPath))
        {
            configContent = await File.ReadAllTextAsync(_configPath);
            config = JsonDocument.Parse(configContent);
        }
        else
        {
            configContent = "{}";
            config = JsonDocument.Parse(configContent);
        }

        var root = config.RootElement;
        JsonElement googleDriveElement;
        root.TryGetProperty("GoogleDrive", out googleDriveElement);

        var googleDrive = googleDriveElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        googleDrive["RefreshToken"] = JsonDocument.Parse($"\"{tokenToSave}\"").RootElement;

        var newConfig = new Dictionary<string, object>();
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name == "GoogleDrive")
            {
                newConfig[prop.Name] = googleDrive;
            }
            else
            {
                newConfig[prop.Name] = prop.Value.Clone();
            }
        }

        var newConfigJson = JsonSerializer.Serialize(newConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configPath, newConfigJson);

        _logger.LogInformation("Refresh token saved to configuration");
    }
}