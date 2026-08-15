using Application.Abstractions;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class UpdateUserRoleCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateUserRoleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Not found user: {request.UserId}");

        if (user.Role == request.Role)
            return Unit.Value;

        user.Role = request.Role;

        await db.Sessions.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
