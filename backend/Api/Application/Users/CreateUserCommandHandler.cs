using Application.Abstractions;
using Domain;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class CreateUserCommandHandler(DashboardDbContext db) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var sites = await db.Sites
            .Where(s => request.Sites.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var companies = await db.Companies
            .Where(c => request.Companies.Contains(c.Id))
            .ToListAsync(cancellationToken);


        var user = User.Create(request.Name, request.Email, sites, companies);

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
