using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class HttpEmailSender(
    IHttpClientFactory httpClientFactory,
    IOptions<HttpEmailOptions> options,
    ILogger<HttpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrEmpty(opts.ApiKey))
            throw new InvalidOperationException("Email:Http:ApiKey is not configured.");
        if (string.IsNullOrEmpty(opts.From))
            throw new InvalidOperationException("Email:Http:From is not configured.");

        var client = httpClientFactory.CreateClient("Resend");
        var request = new ResendSendRequest(
            opts.From,
            [message.To],
            message.Subject,
            message.HtmlBody);

        using var response = await client.PostAsJsonAsync("https://api.resend.com/emails", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Failed to send email to {To}. Status: {Status}. Response: {Body}",
                message.To, (int)response.StatusCode, body);
            throw new HttpRequestException($"Resend API returned {(int)response.StatusCode}: {body}");
        }
    }
}

internal sealed record ResendSendRequest(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string[] To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("html")] string Html);