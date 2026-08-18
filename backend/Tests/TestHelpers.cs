using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tests;

/// <summary>
/// JSON options matching the production wire format: the app configures
/// HttpJsonOptions with JsonSerializerDefaults.Web plus a camelCase
/// JsonStringEnumConverter (see Host/Program.cs), so enums are sent as
/// camelCase strings (e.g. "role":"admin"). Tests must use these options
/// when (de)serializing any DTO that contains an enum.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

public static class TestHelpers
{
    public static async Task<Guid> ReadCreatedIdAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException($"Expected a created resource ID but got empty response (status: {response.StatusCode})");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
            return Guid.Parse(idProp.GetString()!);

        if (root.TryGetProperty("Id", out var idProp2) && idProp2.ValueKind == JsonValueKind.String)
            return Guid.Parse(idProp2.GetString()!);

        throw new InvalidOperationException($"Expected a created resource ID but got: {content}");
    }
}
