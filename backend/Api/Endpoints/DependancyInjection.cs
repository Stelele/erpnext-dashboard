using Api.Endpoints;
using Microsoft.AspNetCore.Builder;

namespace Api;

public static class DependancyInjection
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app.MapUsersEndpoints();
        return app;
    }

    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        return builder;
    }
}
