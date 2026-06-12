using Application.Requests;
using Application.DTOs;
using Application.Sites;
using Infrastructure.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text.Json;

namespace Api.Endpoints;

public static class SitesEndpoints
{
    public static WebApplication MapSitesEndpoints(this WebApplication app)
    {
        app.MapDelete("/sites/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteSiteCommand(id));
            return Results.NoContent();
        })
         .WithName("DeleteSite")
         .WithDisplayName("DeleteSite")
         .Produces(StatusCodes.Status204NoContent)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.DeleteSites);

        app.MapGet("/sites/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var site = await mediator.Send(new GetSiteByIdQuery(id));
            return site is not null ? Results.Ok(site) : Results.NotFound();
        })
         .WithName("GetSiteById")
         .WithDisplayName("GetSiteById")
         .Produces<SiteResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.ReadSites);

        app.MapGet("/sites", async (Guid[]? siteIds, ISender mediator) =>
        {
            var sites = await mediator.Send(new GetSitesQuery(siteIds));
            return Results.Ok(sites);
        })
         .WithName("GetAllSites")
         .WithDisplayName("GetAllSites")
         .Produces<List<SiteResponse>>(StatusCodes.Status200OK)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.ReadSites);

        app.MapPost("/sites", async (CreateSiteRequest request, ISender mediator) =>
        {
            var command = new CreateSiteCommand(request.Name, request.Url, request.Description, request.ApiToken);
            var siteId = await mediator.Send(command);
            return Results.Created($"/sites/{siteId}", new { id = siteId });
        })
         .WithName("CreateSite")
         .WithDisplayName("CreateSite")
         .Accepts<CreateSiteRequest>("application/json")
         .Produces<Guid>(StatusCodes.Status201Created)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.CreateSites);

        app.MapGet("/sites/{siteId:guid}/logo", async (
            Guid siteId,
            string? company,
            DashboardDbContext db,
            IHttpClientFactory httpClientFactory) =>
        {
            var site = await db.Sites.FindAsync(siteId);
            if (site is null) return Results.NotFound();

            var client = httpClientFactory.CreateClient();
            var authHeader = $"token {site.ApiToken}";

            // Get company logo path from ERPNext
            var companyUrl = $"{site.Url}/api/resource/Company/{Uri.EscapeDataString(company ?? "")}";
            var companyRequest = new HttpRequestMessage(HttpMethod.Get, companyUrl);
            companyRequest.Headers.Add("Authorization", authHeader);
            var companyResponse = await client.SendAsync(companyRequest);
            if (!companyResponse.IsSuccessStatusCode) return Results.NotFound();

            var companyJson = await companyResponse.Content.ReadAsStringAsync();
            using var companyDoc = JsonDocument.Parse(companyJson);
            var logoPath = companyDoc.RootElement
                .GetProperty("data")
                .GetProperty("company_logo")
                .GetString();
            if (string.IsNullOrEmpty(logoPath)) return Results.NotFound();

            // Fetch the logo image with auth
            var logoUrl = $"{site.Url}{logoPath}";
            var logoRequest = new HttpRequestMessage(HttpMethod.Get, logoUrl);
            logoRequest.Headers.Add("Authorization", authHeader);
            var logoResponse = await client.SendAsync(logoRequest);
            if (!logoResponse.IsSuccessStatusCode) return Results.NotFound();

            var contentType = logoResponse.Content.Headers.ContentType?.MediaType ?? "image/png";
            var imageBytes = await logoResponse.Content.ReadAsByteArrayAsync();
            return Results.Bytes(imageBytes, contentType);
        })
         .WithName("GetCompanyLogo")
         .WithDisplayName("GetCompanyLogo")
         .Produces(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Sites);

        return app;
    }
}
