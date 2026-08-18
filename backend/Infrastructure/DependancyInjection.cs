using Infrastructure.Email;
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
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IR2StorageService, R2StorageService>();

        var r2 = builder.Services.BuildServiceProvider().GetRequiredService<IR2StorageService>();
        var dbRestoreLogger = builder.Services.BuildServiceProvider()
            .GetRequiredService<ILogger<DatabaseRestoreService>>();
        DatabaseRestoreService.EnsureDatabaseExists(r2, builder.Configuration, dbRestoreLogger);

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
