using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;

namespace Application.Users;

public class GetUsersQueryHandler(DashboardDbContext db) : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = request.UserIds != null && request.UserIds.Length > 0
            ? db.Users.Where(u => request.UserIds.Contains(u.Id)).ToList()
            : db.Users.ToList();

        return users.Select(UserResponse.FromDomain).ToList();
    }
}
