using Auth0.ManagementApi.Models;
using Infrastructure.Auth0;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

public class TestAuth0UserProvisioner : Auth0UserProvisioner
{
    private readonly List<User> _users = [];

    public TestAuth0UserProvisioner()
        : base("test.auth0.com", "test-client", "test-secret", "test-app-client", NullLogger.Instance)
    {
    }

    public override Task<User> CreateUserInConnectionAsync(
        string connectionName,
        string email,
        Guid userId,
        string name,
        bool emailVerified = false,
        CancellationToken ct = default)
    {
        var user = new User { UserId = $"auth0|{userId}", Email = email };
        _users.Add(user);
        return Task.FromResult(user);
    }

    public override Task DeleteUserAsync(string auth0UserId, CancellationToken ct = default)
    {
        _users.RemoveAll(u => u.UserId == auth0UserId);
        return Task.CompletedTask;
    }
}
