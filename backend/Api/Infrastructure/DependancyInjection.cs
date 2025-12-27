using Infrastructure.Auth0;
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

        return builder;
    }
}
