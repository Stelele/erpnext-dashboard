using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Host.Transformers
{
    public class DocumentTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var serverUrl = configuration["OpenApi:ServerUrl"];
            if (!string.IsNullOrEmpty(serverUrl))
            {
                document.Servers = [new() { Url = serverUrl }];
            }

            var bearerScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Enter 'Bearer {your JWT token}'"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = bearerScheme
            };

            var bearerSchemeReference = new OpenApiSecuritySchemeReference("Bearer");
            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [ bearerSchemeReference ] = []
                }
            ];
        }
    }
}
