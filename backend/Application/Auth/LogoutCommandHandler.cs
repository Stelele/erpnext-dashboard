using Application.Abstractions;
using Infrastructure.Auth;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth;

public class LogoutCommandHandler(DashboardDbContext db) : ICommandHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        await db.Sessions
            .Where(s => s.TokenHash == OpaqueToken.Hash(request.Token))
            .ExecuteDeleteAsync(ct);

        return Unit.Value;
    }
}
