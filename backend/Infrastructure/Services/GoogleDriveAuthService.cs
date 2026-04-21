using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class GoogleDriveAuthService
{
    private readonly ILogger _logger;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly IGoogleTokenProvider _tokenProvider;

    public GoogleDriveAuthService(IConfiguration configuration, IGoogleTokenProvider tokenProvider, ILogger logger)
    {
        _logger = logger;
        _clientId = configuration["GoogleDrive:ClientID"];
        _clientSecret = configuration["GoogleDrive:ClientSecret"];
        _tokenProvider = tokenProvider;
    }

    public string GetAuthorizationUrl(string redirectUri)
    {
        var clientId = _clientId ?? throw new InvalidOperationException("GoogleDrive:ClientID is not configured");
        var clientSecret = _clientSecret ?? throw new InvalidOperationException("GoogleDrive:ClientSecret is not configured");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
        });

        var request = flow.CreateAuthorizationCodeRequest(redirectUri);
        request.Scope = "https://www.googleapis.com/auth/drive.file";
        
        return request.Build().AbsoluteUri + "&prompt=consent";
    }

    public async Task<TokenResponse> ExchangeCodeForTokensAsync(string code, string redirectUri, CancellationToken ct)
    {
        var clientId = _clientId ?? throw new InvalidOperationException("GoogleDrive:ClientID is not configured");
        var clientSecret = _clientSecret ?? throw new InvalidOperationException("GoogleDrive:ClientSecret is not configured");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
        });

        return await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, ct);
    }

    public async Task SaveRefreshTokenAsync(string refreshToken, IConfiguration configuration, string contentRootPath)
    {
        _tokenProvider.SetToken(refreshToken);
        await _tokenProvider.SaveToConfigurationAsync(configuration, contentRootPath);
        _logger.LogInformation("Refresh token saved successfully");
    }

    public async Task StartAuthFlowAsync(CancellationToken ct, IConfiguration configuration, string contentRootPath)
    {
        const int port = 51937;
        var redirectUri = $"http://localhost:{port}/callback";
        var authUrl = GetAuthorizationUrl(redirectUri);

        _logger.LogInformation("Google Drive authentication required");
        _logger.LogInformation("Opening browser for authorization...");
        _logger.LogInformation("If browser doesn't open, visit: {AuthUrl}", authUrl);

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open browser automatically. Please open the URL manually.");
        }

        using var listener = new System.Net.HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        _logger.LogInformation("Waiting for authorization... (Press Ctrl+C to cancel)");

        var context = await listener.GetContextAsync();
        var request = context.Request;
        var response = context.Response;

        var code = request.QueryString["code"];
        var error = request.QueryString["error"];

        if (!string.IsNullOrEmpty(error))
        {
            var errorResponse = $"""
                <html>
                <body style="font-family: sans-serif; padding: 40px; text-align: center;">
                    <h1 style="color: #e74c3c;">Authorization Failed</h1>
                    <p style="color: #666;">Error: {WebUtility.HtmlEncode(error)}</p>
                    <p>You may close this window and return to the terminal.</p>
                </body>
                </html>
                """;
            var buffer = System.Text.Encoding.UTF8.GetBytes(errorResponse);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            await response.OutputStream.WriteAsync(buffer, ct);
            response.Close();
            listener.Stop();
            throw new InvalidOperationException($"Authorization failed: {error}");
        }

        if (string.IsNullOrEmpty(code))
        {
            var errorResponse = """
                <html>
                <body style="font-family: sans-serif; padding: 40px; text-align: center;">
                    <h1 style="color: #e74c3c;">No Authorization Code</h1>
                    <p style="color: #666;">No code was received. Please try again.</p>
                </body>
                </html>
                """;
            var buffer = System.Text.Encoding.UTF8.GetBytes(errorResponse);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            await response.OutputStream.WriteAsync(buffer, ct);
            response.Close();
            listener.Stop();
            throw new InvalidOperationException("No authorization code received");
        }

        listener.Stop();

        _logger.LogInformation("Authorization code received. Exchanging for tokens...");

        var tokenResponse = await ExchangeCodeForTokensAsync(code, redirectUri, ct);

        if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            throw new InvalidOperationException("No refresh token in response. Please ensure offline access is granted.");
        }

        await SaveRefreshTokenAsync(tokenResponse.RefreshToken, configuration, contentRootPath);

        _logger.LogInformation("Google Drive authentication successful!");
    }
}