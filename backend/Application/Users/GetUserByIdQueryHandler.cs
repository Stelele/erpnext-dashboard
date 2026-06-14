using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUserByIdQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetUserByIdQuery, UserResponse?>
{
    public async Task<UserResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (userContext.CompanyIds.Count > 0 && request.Id != userContext.UserId)
            return null;

        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        return user == null ? null : UserResponse.FromDomain(user);
    }
}
