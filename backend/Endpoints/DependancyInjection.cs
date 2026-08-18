using System.Threading.RateLimiting;
using Api.Authentication;
using Api.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Api;

public static class DependancyInjection
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app
            .MapCompanyEndpoints()
            .MapSitesEndpoints()
            .MapUsersEndpoints()
            .MapExpenseEndpoints()
            .MapThemeEndpoints()
            .MapAuthEndpoints();

        return app;
    }

    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(SessionAuthDefaults.AuthenticationScheme)
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(
                SessionAuthDefaults.AuthenticationScheme, _ => { });

        builder.Services.AddAuthorizationBuilder()
            .AddPermission(Permissions.ReadUsers)
            .AddPermission(Permissions.CreateUsers)
            .AddPermission(Permissions.UpdateUsers)
            .AddPermission(Permissions.DeleteUsers)

            .AddPermission(Permissions.ReadSites)
            .AddPermission(Permissions.CreateSites)
            .AddPermission(Permissions.UpdateSites)
            .AddPermission(Permissions.DeleteSites)

            .AddPermission(Permissions.ReadCompanies)
            .AddPermission(Permissions.CreateCompanies)
            .AddPermission(Permissions.UpdateCompanies)
            .AddPermission(Permissions.DeleteCompanies)

            .AddPermission(Permissions.ReadExpenses)
            .AddPermission(Permissions.CreateExpenses)
            .AddPermission(Permissions.UpdateExpenses)
            .AddPermission(Permissions.DeleteExpenses);

        builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthRateLimit.PolicyName, context =>
            {
                var permitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
                var window = builder.Configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(window),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                string[] origins = builder.Configuration["Cors:AllowedOrigin"]?.Split(';')
                    ?? throw new InvalidOperationException("Allowed origin is not configured.");

                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return builder;
    }

    private static AuthorizationBuilder AddPermission(this AuthorizationBuilder builder, string permission)
    {
        return builder.AddPolicy(permission, p => p.RequireAuthenticatedUser().AddRequirements(new HasScopeRequirement(permission)));
    }
}

public static class AuthRateLimit
{
    public const string PolicyName = "auth";
}
