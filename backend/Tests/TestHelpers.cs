using System.Net;
using System.Text.Json;

namespace Tests;

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
