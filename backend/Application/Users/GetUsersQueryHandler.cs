using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUsersQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.Include(u => u.Companies).AsQueryable();

        if (userContext.CompanyIds.Count > 0)
            query = query.Where(u => u.Id == userContext.UserId);

        if (request.UserIds != null && request.UserIds.Length > 0)
            query = query.Where(u => request.UserIds.Contains(u.Id));

        return [.. query.Select(UserResponse.FromDomain)];
    }
}
