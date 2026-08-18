using Infrastructure.Email;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependancyInjection
{
    public static WebApplication MapInfrastructure(this WebApplication app)
    {
        var r2 = app.Services.GetRequiredService<IR2StorageService>();
        var restoreLogger = app.Services.GetRequiredService<ILogger<DatabaseRestoreService>>();
        DatabaseRestoreService.EnsureDatabaseExists(r2, app.Configuration, restoreLogger);

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

        // Pre-warm EF Core to avoid cold-start penalty on first request
        try
        {
            db.Companies.FirstOrDefault();
            db.CompanySettings.FirstOrDefault();
            app.Logger.LogInformation("EF Core warmed up successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "EF Core warm-up query failed (non-critical)");
        }

        return app;
    }

    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email:Smtp"));
        builder.Services.Configure<HttpEmailOptions>(builder.Configuration.GetSection("Email:Http"));
        builder.Services.AddHttpClient("Resend", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HttpEmailOptions>>().Value;
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
        });
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IEmailSender, HttpEmailSender>();
        builder.Services.AddSingleton<IR2StorageService, R2StorageService>();

        builder.Services.AddDbContext<DashboardDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Sqlite") ??
                throw new InvalidOperationException("Connection string 'Sqlite' not found.");
                
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
