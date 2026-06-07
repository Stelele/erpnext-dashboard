using Application.Abstractions;
using Domain.Exceptions;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record UpdateExpenseTypeCommand(
    Guid Id,
    string Name,
    string Description
) : ICommand;

public class UpdateExpenseTypeValidator : AbstractValidator<UpdateExpenseTypeCommand>
{
    public UpdateExpenseTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class UpdateExpenseTypeCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateExpenseTypeCommand>
{
    public async Task Handle(UpdateExpenseTypeCommand request, CancellationToken ct)
    {
        var expenseType = await db.ExpenseTypes
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException($"Expense type not found: {request.Id}");

        var nameExists = await db.ExpenseTypes
            .AnyAsync(e => e.Name == request.Name && e.Id != request.Id, ct);

        if (nameExists)
            throw new DuplicateDomainMemberException($"An expense type with the name '{request.Name}' already exists.");

        expenseType.Name = request.Name;
        expenseType.Description = request.Description;
        expenseType.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
