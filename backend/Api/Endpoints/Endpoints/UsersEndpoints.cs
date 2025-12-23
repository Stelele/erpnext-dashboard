using Application.DTOs;
using Application.Users;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        app.MapGet("/users/{id}", async (Guid id, ISender mediator) =>
        {
            var user = await mediator.Send(new GetUserByIdQuery(id));
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
         .WithName("GetUserById")
         .WithDisplayName("GetUserById")
         .Produces<UserResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Users);

        app.MapGet("/users", async (Guid[]? ids, ISender mediator) =>
        {
            var users = await mediator.Send(new GetUsersQuery(ids));
            return Results.Ok(users);
        })
         .WithName("GetUsers")
         .WithName("GetUsers")
         .Produces<List<UserResponse>>(StatusCodes.Status200OK)
         .WithTags(Tags.Users);

        app.MapPost("/users", async (CreateUserCommand command, ISender mediator) =>
        {
            var userId = await mediator.Send(command);
            return Results.Created($"/users/{userId}", new { Id = userId });
        })
         .WithName("CreateUser")
         .WithDisplayName("CreateUser")
         .Accepts<CreateUserCommand>("application/json")
         .Produces<Guid>(StatusCodes.Status201Created)
         .WithTags(Tags.Users);
    }
}
