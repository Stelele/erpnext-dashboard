using Application.Users;
using Domain.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

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
            var userIdClaim = context.User.FindFirst("user_id");
            if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await db.Users
                    .AsNoTracking()
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    userContext.UserId = user.Id;
                    userContext.IsAdmin = user.Role == Role.Admin;
                    userContext.CompanyIds = user.Companies.Select(c => c.Id).ToList();
                }
            }
        }

        await _next(context);
    }
}
