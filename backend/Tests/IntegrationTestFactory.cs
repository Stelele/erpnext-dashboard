using System.Data.Common;
using Application;
using Api;
using Api.Authentication;
using Infrastructure;
using Infrastructure.Auth0;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _sharedConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Create and open a shared in-memory connection
        _sharedConnection = new SqliteConnection("Data Source=:memory:");
        _sharedConnection.Open();

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:Domain"] = "test.auth0.com",
                ["Auth0:Audience"] = "test-api",
                ["Auth0:Apps:ERP-Dashboard"] = "test-client-id",
                ["Identity:ClientId"] = "test-client-id",
                ["Identity:ClientSecret"] = "test-client-secret",
                ["MediatR:LicenseKey"] = "",
                ["ConnectionStrings:Sqlite"] = "Data Source=:memory:"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace Auth with test handler
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            services.AddAuthorization(options =>
            {
                var permissions = new[]
                {
                    "read:users", "create:users", "update:users", "delete:users",
                    "read:companies", "create:companies", "update:companies", "delete:companies",
                    "read:sites", "create:sites", "update:sites", "delete:sites",
                    "read:expenses", "create:expenses", "update:expenses", "delete:expenses",
                };

                foreach (var permission in permissions)
                {
                    options.AddPolicy(permission, p =>
                        p.RequireAuthenticatedUser()
                         .AddRequirements(new HasScopeRequirement(permission)));
                }

                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

            // Replace external dependencies with stubs
            services.AddSingleton<IR2StorageService, StubR2StorageService>();
            services.AddSingleton<Auth0UserProvisioner, TestAuth0UserProvisioner>();

            // Replace DbContext with in-memory SQLite using shared connection
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DashboardDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<DashboardDbContext>(options =>
                options.UseSqlite(_sharedConnection));

            // Remove hosted services (DatabaseSyncService)
            var hostedServices = services
                .Where(d => typeof(IHostedService).IsAssignableFrom(d.ServiceType)
                         || d.ServiceType == typeof(DatabaseSyncService))
                .ToList();
            foreach (var hosted in hostedServices)
                services.Remove(hosted);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Migrate the database after the host is built (connection is active)
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        db.Database.Migrate();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        _sharedConnection?.Dispose();
        base.Dispose(disposing);
    }

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        return client;
    }

    /// <summary>
    /// Clears all data from the database. Call this at the start of each test
    /// to ensure test isolation with the shared in-memory connection.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();

        db.CompanyExpenseMappings.RemoveRange(db.CompanyExpenseMappings);
        db.CompanySettings.RemoveRange(db.CompanySettings);
        db.Companies.RemoveRange(db.Companies);
        db.Sites.RemoveRange(db.Sites);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }
}
