using Application.Abstractions;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record DeleteExpenseTypeCommand(Guid Id) : ICommand;

public class DeleteExpenseTypeCommandHandler(DashboardDbContext db) : ICommandHandler<DeleteExpenseTypeCommand>
{
    public async Task Handle(DeleteExpenseTypeCommand request, CancellationToken ct)
    {
        var expenseType = await db.ExpenseTypes
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException($"Expense type not found: {request.Id}");

        expenseType.IsDeleted = true;
        expenseType.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
