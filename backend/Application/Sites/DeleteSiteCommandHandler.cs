using Application.Abstractions;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class DeleteSiteCommandHandler(
    DashboardDbContext db
) : ICommandHandler<DeleteSiteCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No site found with matching id: {request.Id}");

        db.Sites.Remove(site);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
