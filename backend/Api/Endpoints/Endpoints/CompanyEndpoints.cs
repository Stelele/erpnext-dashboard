using Application.Companies;
using Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class CompanyEndpoints
{
    public static WebApplication MapCompanyEndpoints(this WebApplication app)
    {
        app.MapGet("/companies", async (IMediator mediator, Guid[]? companyIds) =>
        {
            var query = new GetCompaniesQuery(companyIds);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithTags(Tags.Companies)
        .WithName("GetCompanies")
        .WithDescription("Retrieves a list of companies. Optionally filter by an array of company IDs.")
        .Produces<List<CompanyResponse>>(StatusCodes.Status200OK);

        app.MapGet("/companies/{id:guid}", async (IMediator mediator, Guid id) =>
        {
            var query = new GetCompanyByIdQuery(id);
            var result = await mediator.Send(query);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithTags(Tags.Companies)
        .WithName("GetCompanyById")
        .WithDescription("Retrieves a company by its unique identifier.")
        .Produces<ExtendedCompanyResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/companies/{id:guid}", async (IMediator mediator, Guid id) =>
        {
            var command = new DeleteCompanyCommand(id);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Companies)
        .WithName("DeleteCompany")
        .WithDescription("Deletes a company by its unique identifier.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/companies", async (IMediator mediator, CreateCompanyCommand command) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/companies/{result}", result);
        })
        .WithTags(Tags.Companies)
        .WithName("CreateCompany")
        .WithDescription("Creates a new company with the provided details.")
        .Accepts<CreateCompanyCommand>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created);

        return app;
    }
}
