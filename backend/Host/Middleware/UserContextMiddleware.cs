using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Host.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserContext userContext, DashboardDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var namespaceClaim = context.User.FindFirst("https://meta.dashboard.com/");
            if (namespaceClaim != null)
            {
                using var doc = JsonDocument.Parse(namespaceClaim.Value);
                if (doc.RootElement.TryGetProperty("user_id", out var uid) && uid.TryGetGuid(out var userId))
                {
                    var user = await db.Users
                        .AsNoTracking()
                        .Include(u => u.Companies)
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user != null)
                    {
                        userContext.UserId = user.Id;
                        userContext.CompanyIds = user.Companies.Select(c => c.Id).ToList();
                    }
                }
            }
        }

        await _next(context);
    }
}
