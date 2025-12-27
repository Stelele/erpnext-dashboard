using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;

namespace Application.Users;

public class GetUserByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetUserByIdQuery, UserResponse?>
{
    public async Task<UserResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([request.Id], cancellationToken);
        if (user == null) return null;
        return UserResponse.FromDomain(user);
    }
}
