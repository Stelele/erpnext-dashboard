using Infrastructure.Auth0;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class DependancyInjection
{
    public static WebApplication MapInfrastructure(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        var canConnect = db.Database.CanConnect();
        app.Logger.LogInformation("Can connect to database: {CanConnect}", canConnect);

        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }

        return app;
    }

    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        var loggerFactory = builder.Services.BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>();
        var tokenProviderLogger = loggerFactory.CreateLogger<GoogleTokenProvider>();
        var tokenProvider = new GoogleTokenProvider(tokenProviderLogger);
        var contentRoot = builder.Environment.ContentRootPath;
        tokenProvider.LoadFromConfigurationAsync(builder.Configuration, contentRoot).GetAwaiter().GetResult();

        GoogleDriveService? googleDrive = null;

        if (!tokenProvider.HasToken)
        {
            var mainLogger = loggerFactory.CreateLogger("GoogleDriveAuth");
            mainLogger.LogInformation("No Google Drive token found. Starting authentication...");
            var authLogger = loggerFactory.CreateLogger<GoogleDriveAuthService>();
            var authService = new GoogleDriveAuthService(builder.Configuration, tokenProvider, authLogger);
            authService.StartAuthFlowAsync(CancellationToken.None, builder.Configuration, contentRoot).GetAwaiter().GetResult();
        }
        else
        {
            googleDrive = new GoogleDriveService(builder.Configuration, tokenProvider);
            var isValid = googleDrive.ValidateTokenAsync(
                CancellationToken.None,
                TimeSpan.FromSeconds(10)
            ).GetAwaiter().GetResult();

            if (!isValid)
            {
                var mainLogger = loggerFactory.CreateLogger("GoogleDriveAuth");
                mainLogger.LogWarning("Google Drive token invalid or expired. Re-authenticating...");
                var authLogger = loggerFactory.CreateLogger<GoogleDriveAuthService>();
                var authService = new GoogleDriveAuthService(builder.Configuration, tokenProvider, authLogger);
                authService.StartAuthFlowAsync(CancellationToken.None, builder.Configuration, contentRoot).GetAwaiter().GetResult();
            }
            else
            {
                var mainLogger = loggerFactory.CreateLogger("GoogleDriveAuth");
                mainLogger.LogInformation("Google Drive token is valid.");
            }
        }

        googleDrive ??= new GoogleDriveService(builder.Configuration, tokenProvider);

        builder.Services.AddSingleton<IGoogleTokenProvider>(tokenProvider);
        builder.Services.AddSingleton(googleDrive);

        builder.Services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Auth0UserProvisioner>>();
            return new Auth0UserProvisioner(
                domain: builder.Configuration["Auth0:Domain"] ?? throw new InvalidOperationException("Auth0:Domain is null"),
                clientId: builder.Configuration["Identity:ClientId"] ?? throw new InvalidOperationException("Identity:ClientId is null"),
                clientSecret: builder.Configuration["Identity:ClientSecret"] ?? throw new InvalidOperationException("Identity:ClientSecret is null"),
                auth0AppClientId: builder.Configuration["Auth0:Apps:ERP-Dashboard"] ?? throw new InvalidOperationException("Auth0:Apps:ERP-Dashboard is null"),
                logger: logger);
        });

        var dbRestoreLogger = builder.Services.BuildServiceProvider()
            .GetRequiredService<ILogger<DatabaseRestoreService>>();
        DatabaseRestoreService.EnsureDatabaseExists(googleDrive, builder.Configuration, dbRestoreLogger);

        builder.Services.AddDbContext<DashboardDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Sqlite") ??
                "Data Source=erpnext.db";
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        builder.Services.AddSingleton<IDatabaseSyncService, DatabaseSyncService>();
        builder.Services.AddHostedService(sp => (DatabaseSyncService)sp.GetRequiredService<IDatabaseSyncService>());

        builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

        return builder;
    }
}
