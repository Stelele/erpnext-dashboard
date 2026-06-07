using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Host.Transformers;

public class NullableSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        // Fix nullable for properties that are declared as non-nullable
        if (context.JsonPropertyInfo is { } jsonPropertyInfo)
        {
            // Get the underlying PropertyInfo from JsonPropertyInfo
            var propertyInfo = jsonPropertyInfo.AttributeProvider as PropertyInfo;
            if (propertyInfo is not null)
            {
                var nullabilityContext = new NullabilityInfoContext();
                var nullabilityInfo = nullabilityContext.Create(propertyInfo);

                if (nullabilityInfo.ReadState == NullabilityState.NotNull)
                {
                    // In OpenAPI 3.1, nullable types use type array like ["string", "null"]
                    // Remove "null" from the type array for non-nullable properties
                    if (schema.Type.HasValue)
                    {
                        var typeValue = schema.Type.Value;
                        if (typeValue.HasFlag(JsonSchemaType.Null))
                        {
                            var nonNullType = typeValue & ~JsonSchemaType.Null;
                            if (nonNullType != 0)
                            {
                                schema.Type = nonNullType;
                            }
                        }
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
