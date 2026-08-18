using Application.Abstractions;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class DeleteUserCommandHandler(DashboardDbContext db) : ICommandHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Not found user: {request.Id}");

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
