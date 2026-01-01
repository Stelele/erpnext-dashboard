using Api.Authentication;
using Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace Api;

public static class DependancyInjection
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        app
            .MapCompanyEndpoints()
            .MapSitesEndpoints()
            .MapUsersEndpoints();

        app.UseCors("AllowFrontend");

        return app;
    }

    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, c =>
            {
                c.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
                c.Audience = builder.Configuration["Auth0:Audience"];
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPermission(Permissions.ReadUsers)
            .AddPermission(Permissions.UpdateUsers)
            .AddPermission(Permissions.DeleteUsers)

            .AddPermission(Permissions.ReadSites)
            .AddPermission(Permissions.UpdateSites)
            .AddPermission(Permissions.DeleteSites)

            .AddPermission(Permissions.ReadCompanies)
            .AddPermission(Permissions.UpdateCompanies)
            .AddPermission(Permissions.DeleteCompanies);

        builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins("*")
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
