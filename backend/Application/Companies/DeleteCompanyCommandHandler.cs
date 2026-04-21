using Application.Abstractions;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class DeleteCompanyCommandHandler(
    DashboardDbContext db
) : ICommandHandler<DeleteCompanyCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Company with given Id not found: {request.Id}");

        db.Companies.Remove(company);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
