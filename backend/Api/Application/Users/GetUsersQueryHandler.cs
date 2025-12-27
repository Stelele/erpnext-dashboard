using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUsersQueryHandler(DashboardDbContext db) : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = request.UserIds != null && request.UserIds.Length > 0
            ? await db.Users.Where(u => request.UserIds.Contains(u.Id)).ToListAsync(cancellationToken)
            : await db.Users.ToListAsync(cancellationToken);

        return [.. users.Select(UserResponse.FromDomain)];
    }
}
