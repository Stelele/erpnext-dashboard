using Application.DTOs;
using Application.Sites;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
         .Produces<ExtendedSiteResponse>(StatusCodes.Status200OK)
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

        app.MapPost("/sites", async (CreateSiteCommand command, ISender mediator) =>
        {
            var siteId = await mediator.Send(command);
            return Results.Created($"/users/{siteId}", new { id = siteId });
        })
         .WithName("CreateSite")
         .WithDisplayName("CreateSite")
         .Accepts<CreateSiteCommand>("application/json")
         .Produces<Guid>(StatusCodes.Status201Created)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.UpdateSites);

        return app;
    }
}
