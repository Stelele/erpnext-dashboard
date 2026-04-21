using Domain.Exceptions;
using FluentValidation;
using System.Text.Json;

namespace Host.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        context.Response.ContentType = "application/json";
        var (statusCode, payload) = GetStatusCodeAndPayload(exception);
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload)
        );
    }

    private static (int, Problem) GetStatusCodeAndPayload(Exception exception)
    {
        return exception switch
        {
            ValidationException vex => (
                StatusCodes.Status400BadRequest,
                new Problem
                {
                    Title = "Validation failed",
                    Status = 400,
                    Errors = vex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => (object)g.Select(e => e.ErrorMessage).ToArray()
                        )
                }
            ),

            ArgumentException aex => (
                StatusCodes.Status400BadRequest,
                new Problem
                {
                    Title = "Invalid argument",
                    Status = 400,
                    Detail = aex.Message
                }
            ),

            DuplicateDomainMemberException ddme => (
                StatusCodes.Status409Conflict,
                new Problem
                {
                    Title = "Duplicate domain member",
                    Status = 409,
                    Detail = ddme.Message
                }
            ),

            KeyNotFoundException knf => (
                StatusCodes.Status404NotFound,
                new Problem
                {
                    Title = "Resource not found",
                    Status = 404,
                    Detail = knf.Message
                }
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new Problem
                {
                    Title = "Unauthorized",
                    Status = 401
                }
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                new Problem
                {
                    Title = "Internal server error",
                    Status = 500,
                    Detail = "An unexpected error occurred"
                }
            )
        };
    }
}

public sealed class Problem
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Detail { get; set; }
    public Dictionary<string, object>? Errors { get; set; }
}
