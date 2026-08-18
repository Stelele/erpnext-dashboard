using Application.Auth;
using Application.DTOs;
using Application.Requests;
using Application.Users;
using Api.Authentication;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, ISender mediator) =>
        {
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password));
            return Results.Ok(result);
        })
         .WithName("Login")
         .WithDisplayName("Login")
         .Accepts<LoginRequest>("application/json")
         .Produces<LoginResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status401Unauthorized)
         .WithTags(Tags.Auth)
         .AllowAnonymous()
         .RequireRateLimiting(AuthRateLimit.PolicyName);

        app.MapPost("/auth/logout", async (HttpContext http, ISender mediator) =>
        {
            var token = BearerToken.Extract(http.Request.Headers.Authorization.ToString());
            if (token is not null)
                await mediator.Send(new LogoutCommand(token));
            return Results.NoContent();
        })
         .WithName("Logout")
         .WithDisplayName("Logout")
         .Produces(StatusCodes.Status204NoContent)
         .WithTags(Tags.Auth)
         .RequireAuthorization();

        app.MapGet("/auth/me", async (HttpContext http, ISender mediator) =>
        {
            var userIdClaim = http.User.FindFirst("user_id");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Results.Unauthorized();

            var user = await mediator.Send(new GetUserByIdQuery(userId));
            return user is not null ? Results.Ok(user) : Results.Unauthorized();
        })
         .WithName("GetCurrentUser")
         .WithDisplayName("GetCurrentUser")
         .Produces<UserResponse>(StatusCodes.Status200OK)
         .WithTags(Tags.Auth)
         .RequireAuthorization();

        app.MapPost("/auth/forgot-password", async (ForgotPasswordRequest request, ISender mediator) =>
        {
            await mediator.Send(new ForgotPasswordCommand(request.Email));
            return Results.Ok();
        })
         .WithName("ForgotPassword")
         .WithDisplayName("ForgotPassword")
         .Accepts<ForgotPasswordRequest>("application/json")
         .Produces(StatusCodes.Status200OK)
         .WithTags(Tags.Auth)
         .AllowAnonymous()
         .RequireRateLimiting(AuthRateLimit.PolicyName);

        app.MapPost("/auth/reset-password", async (ResetPasswordRequest request, ISender mediator) =>
        {
            var result = await mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
            return Results.Ok(result);
        })
         .WithName("ResetPassword")
         .WithDisplayName("ResetPassword")
         .Accepts<ResetPasswordRequest>("application/json")
         .Produces<LoginResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status401Unauthorized)
         .WithTags(Tags.Auth)
         .AllowAnonymous()
         .RequireRateLimiting(AuthRateLimit.PolicyName);

        return app;
    }
}
