using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUserByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetUserByIdQuery, ExtendedUserResponse?>
{
    public async Task<ExtendedUserResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null) return null;

        return ExtendedUserResponse.FromDomain(user);
    }
}
