using Application.DTOs;
using Application.Features;
using Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class FeatureEndpoints
{
    public static WebApplication MapFeatureEndpoints(this WebApplication app)
    {
        app.MapGet("/features", async (IMediator mediator) =>
        {
            var query = new GetFeaturesQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithTags(Tags.Features)
        .WithName("GetFeatures")
        .WithDescription("Retrieves a list of features in the feature registry.")
        .Produces<List<FeatureResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization(Permissions.ReadFeatures);

        app.MapPost("/features", async (IMediator mediator, CreateFeatureRequest request) =>
        {
            var command = new CreateFeatureCommand(request.Code, request.Name, request.Description);
            var result = await mediator.Send(command);
            return Results.Created($"/features/{result}", new { id = result });
        })
        .WithTags(Tags.Features)
        .WithName("CreateFeature")
        .WithDescription("Creates a new feature in the feature registry.")
        .Accepts<CreateFeatureRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created)
        .RequireAuthorization(Permissions.ManageFeatures);

        app.MapPut("/features/{id:guid}", async (IMediator mediator, Guid id, UpdateFeatureRequest request) =>
        {
            var command = new UpdateFeatureCommand(id, request.Name, request.Description);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Features)
        .WithName("UpdateFeature")
        .WithDescription("Updates a feature's name and description. The code is immutable.")
        .Accepts<UpdateFeatureRequest>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(Permissions.ManageFeatures);

        app.MapDelete("/features/{id:guid}", async (IMediator mediator, Guid id) =>
        {
            var command = new DeleteFeatureCommand(id);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Features)
        .WithName("DeleteFeature")
        .WithDescription("Soft-deletes a feature that is not referenced by any recipe.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(Permissions.ManageFeatures);

        app.MapGet("/recipes", async (IMediator mediator) =>
        {
            var query = new GetRecipesQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithTags(Tags.Features)
        .WithName("GetRecipes")
        .WithDescription("Retrieves a list of recipes with their features.")
        .Produces<List<RecipeResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization(Permissions.ReadFeatures);

        app.MapPost("/recipes", async (IMediator mediator, CreateRecipeRequest request) =>
        {
            var command = new CreateRecipeCommand(request.Name, request.Description, request.FeatureIds);
            var result = await mediator.Send(command);
            return Results.Created($"/recipes/{result}", new { id = result });
        })
        .WithTags(Tags.Features)
        .WithName("CreateRecipe")
        .WithDescription("Creates a new recipe with its feature set.")
        .Accepts<CreateRecipeRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created)
        .RequireAuthorization(Permissions.ManageFeatures);

        app.MapPut("/recipes/{id:guid}", async (IMediator mediator, Guid id, UpdateRecipeRequest request) =>
        {
            var command = new UpdateRecipeCommand(id, request.Name, request.Description, request.FeatureIds);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Features)
        .WithName("UpdateRecipe")
        .WithDescription("Updates a recipe, replacing its feature set.")
        .Accepts<UpdateRecipeRequest>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(Permissions.ManageFeatures);

        app.MapDelete("/recipes/{id:guid}", async (IMediator mediator, Guid id) =>
        {
            var command = new DeleteRecipeCommand(id);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Features)
        .WithName("DeleteRecipe")
        .WithDescription("Soft-deletes a recipe that is not assigned to any company.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(Permissions.ManageFeatures);

        app.MapGet("/companies/{companyId:guid}/features", async (IMediator mediator, Guid companyId) =>
        {
            var query = new GetCompanyFeaturesQuery(companyId);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithTags(Tags.Features)
        .WithName("GetCompanyFeatures")
        .WithDescription("Retrieves the effective feature codes for a company (union across its recipes).")
        .Produces<List<string>>(StatusCodes.Status200OK)
        .RequireAuthorization(Permissions.ReadCompanies);

        app.MapGet("/companies/{companyId:guid}/recipes", async (IMediator mediator, Guid companyId) =>
        {
            var query = new GetCompanyRecipesQuery(companyId);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithTags(Tags.Features)
        .WithName("GetCompanyRecipes")
        .WithDescription("Retrieves the recipes assigned to a company with their features.")
        .Produces<List<RecipeResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization(Permissions.ReadCompanies);

        app.MapPut("/companies/{companyId:guid}/recipes", async (IMediator mediator, Guid companyId, SetCompanyRecipesRequest request) =>
        {
            var command = new SetCompanyRecipesCommand(companyId, request.RecipeIds);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Features)
        .WithName("SetCompanyRecipes")
        .WithDescription("Replaces the set of recipes assigned to a company.")
        .Accepts<SetCompanyRecipesRequest>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization(Permissions.UpdateCompanies);

        return app;
    }
}
