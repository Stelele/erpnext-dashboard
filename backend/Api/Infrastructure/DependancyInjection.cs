using Infrastructure.Models;
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
        app.Logger.LogInformation($"Can connect to database: {canConnect}");

        return app;
    }

    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<DashboardDbContext>((options) =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Postgres") ??
                  throw new InvalidOperationException("Connection string 'Postgres' not found");
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        });

        return builder;
    }
}
