using Application.CompanyExpenseMappings;
using Application.CompanySettings;
using Application.ExpenseTypes;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class ExpenseEndpoints
{
    public static WebApplication MapExpenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags(Tags.Expenses);

        group.MapGet("/expense-types", async (IMediator mediator) =>
                await mediator.Send(new GetExpenseTypesQuery()))
            .Produces<List<Application.DTOs.ExpenseTypeResponse>>(StatusCodes.Status200OK)
            .WithName("GetExpenseTypes")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapGet("/companies/{companyId:guid}/expense-mappings", async (Guid companyId, IMediator mediator) =>
                await mediator.Send(new GetCompanyExpenseMappingsQuery(companyId)))
            .Produces<List<Application.DTOs.CompanyExpenseMappingResponse>>(StatusCodes.Status200OK)
            .WithName("GetCompanyExpenseMappings")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapPut("/companies/{companyId:guid}/expense-mappings", async (Guid companyId, UpsertCompanyExpenseMappingsCommand command, IMediator mediator) =>
            {
                if (command.CompanyId != companyId)
                    return Results.BadRequest();

                await mediator.Send(command);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("UpsertCompanyExpenseMappings")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapGet("/companies/{companyId:guid}/settings", async (Guid companyId, IMediator mediator) =>
            {
                var settings = await mediator.Send(new GetCompanySettingsQuery(companyId));
                return settings == null ? Results.NotFound() : Results.Ok(settings);
            })
            .Produces<Application.DTOs.CompanySettingsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetCompanySettings")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapPut("/companies/{companyId:guid}/settings", async (Guid companyId, UpdateCompanySettingsCommand command, IMediator mediator) =>
            {
                if (command.CompanyId != companyId)
                    return Results.BadRequest();

                await mediator.Send(command);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("UpdateCompanySettings")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapPost("/expense-types", async (IMediator mediator, CreateExpenseTypeCommand command) =>
            {
                var id = await mediator.Send(command);
                return Results.Created($"/api/expense-types/{id}", new { id });
            })
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("CreateExpenseType")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapGet("/expense-types/{id:guid}", async (IMediator mediator, Guid id) =>
            {
                var result = await mediator.Send(new GetExpenseTypeByIdQuery(id));
                return result == null ? Results.NotFound() : Results.Ok(result);
            })
            .Produces<Application.DTOs.ExpenseTypeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetExpenseTypeById")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapPut("/expense-types/{id:guid}", async (IMediator mediator, Guid id, UpdateExpenseTypeCommand command) =>
            {
                if (command.Id != id)
                    return Results.BadRequest();

                await mediator.Send(command);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("UpdateExpenseType")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapDelete("/expense-types/{id:guid}", async (IMediator mediator, Guid id) =>
            {
                await mediator.Send(new DeleteExpenseTypeCommand(id));
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("DeleteExpenseType")
            .RequireAuthorization(Permissions.DeleteExpenses);

        return app;
    }
}
