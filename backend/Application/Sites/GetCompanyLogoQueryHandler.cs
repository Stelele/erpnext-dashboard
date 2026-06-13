using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;

namespace Application.Sites;

public class GetCompanyLogoQueryHandler(
    DashboardDbContext db,
    IHttpClientFactory httpClientFactory,
    IR2StorageService r2,
    IConfiguration configuration
) : IQueryHandler<GetCompanyLogoQuery, LogoResponse?>
{
    public async Task<LogoResponse?> Handle(GetCompanyLogoQuery request, CancellationToken ct)
    {
        var safeCompanyName = request.Company.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        var r2Key = $"dashboard/sites/{request.SiteId}/logo_{safeCompanyName}.png";

        var cdnBaseUrl = configuration["R2:CDNBaseUrl"];
        if (string.IsNullOrEmpty(cdnBaseUrl))
            throw new InvalidOperationException("CDN base URL not configured");
        if (!cdnBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !cdnBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            cdnBaseUrl = $"https://{cdnBaseUrl}";

        // Check R2 first
        if (await r2.ObjectExistsAsync(r2.PublicBucket, r2Key, ct))
            return new LogoResponse($"{cdnBaseUrl}/{r2Key}");

        // Not in R2 — fetch from ERPNext
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId, ct);
        if (site is null) return null;

        var client = httpClientFactory.CreateClient();
        var authHeader = $"token {site.ApiToken}";

        var companyUrl = $"{site.Url}/api/resource/Company/{Uri.EscapeDataString(request.Company)}";
        var companyRequest = new HttpRequestMessage(HttpMethod.Get, companyUrl);
        companyRequest.Headers.Add("Authorization", authHeader);
        var companyResponse = await client.SendAsync(companyRequest, ct);
        if (!companyResponse.IsSuccessStatusCode) return null;

        var companyJson = await companyResponse.Content.ReadAsStringAsync(ct);
        using var companyDoc = JsonDocument.Parse(companyJson);
        var logoPath = companyDoc.RootElement
            .GetProperty("data")
            .GetProperty("company_logo")
            .GetString();
        if (string.IsNullOrEmpty(logoPath)) return null;

        var logoUrl = $"{site.Url}{logoPath}";
        var logoRequest = new HttpRequestMessage(HttpMethod.Get, logoUrl);
        logoRequest.Headers.Add("Authorization", authHeader);
        var logoResponse = await client.SendAsync(logoRequest, ct);
        if (!logoResponse.IsSuccessStatusCode) return null;

        var contentType = logoResponse.Content.Headers.ContentType?.MediaType ?? "image/png";
        var imageStream = await logoResponse.Content.ReadAsStreamAsync(ct);

        await r2.UploadAsync(r2.PublicBucket, r2Key, imageStream, contentType, ct);

        return new LogoResponse($"{cdnBaseUrl}/{r2Key}");
    }
}
