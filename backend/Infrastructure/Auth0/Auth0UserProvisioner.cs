using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Infrastructure.Auth0;


public sealed class Auth0UserProvisioner(
    string domain,
    string clientId,
    string clientSecret,
    string auth0AppClientId,
    ILogger logger)
{
    private readonly string _domain = domain;
    private readonly string _mgmtAudience = $"https://{domain}/api/v2/";
    private readonly string _clientId = clientId;
    private readonly string _clientSecret = clientSecret;
    private readonly string _auth0AppClientId = auth0AppClientId;

    public async Task<User> CreateUserInConnectionAsync(
        string connectionName,
        string email,
        Guid userId,
        string name,
        bool emailVerified = false,
        CancellationToken ct = default)
    {
        var (authClient, mgmt) = await GetClients(ct);

        var request = new UserCreateRequest
        {
            Connection = connectionName,
            Email = email,
            Password = TemporaryPasswordGenerator.Generate(32),
            EmailVerified = emailVerified,
            UserMetadata = new
            {
                display_name = name,
                user_id = userId,
                email
            }
        };

        var auth0User = await mgmt.Users.CreateAsync(request, ct);
        await authClient.ChangePasswordAsync(new ChangePasswordRequest
        {
            ClientId = _auth0AppClientId,
            Email = email,
            Connection = connectionName
        });

        return auth0User;
    }

    private async Task<(AuthenticationApiClient authClient, ManagementApiClient mgmt)> GetClients(CancellationToken ct)
    {
        logger.LogInformation("Creating Auth0 client");
        var authClient = new AuthenticationApiClient(new Uri($"https://{_domain}"));

        var token = await authClient.GetTokenAsync(new ClientCredentialsTokenRequest
        {
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            Audience = _mgmtAudience
        }, ct);
        logger.LogInformation($"Obtained Auth0 management API token: {token.AccessToken}");

        var mgmt = new ManagementApiClient(token.AccessToken, new Uri(_mgmtAudience));
        return (authClient, mgmt);
    }

    public async Task DeleteUserAsync(string auth0UserId, CancellationToken ct = default)
    {
        var (_, mgt) = await GetClients(ct);
        await mgt.Users.DeleteAsync(auth0UserId, ct);
    }
}

public static class TemporaryPasswordGenerator
{
    // Includes upper, lower, digit, and symbol (Auth0-friendly)
    private const string Allowed =
        "ABCDEFGHJKLMNPQRSTUVWXYZ" +
        "abcdefghijkmnopqrstuvwxyz" +
        "23456789" +
        "!@#$%^&*()_+-=";

    public static string Generate(int length = 16)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            chars[i] = Allowed[bytes[i] % Allowed.Length];
        }

        return new string(chars);
    }
}

