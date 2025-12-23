using Application.PipelineBehaviours;
using Application.Users;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependancyInjection
    {
        public static WebApplication MapApplication(this WebApplication app)
        {
            return app;
        }

        public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
        {
            builder.Services.AddMediatR(cfg =>
            {
                cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"];
                cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly);
            });
            builder.Services.AddValidatorsFromAssembly(typeof(CreateUserCommand).Assembly);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return builder;
        }
    }
}
