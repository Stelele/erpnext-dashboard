using Application.Abstractions;
using Domain.Exceptions;
using Domain.ExpenseTypes;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record CreateExpenseTypeCommand(
    string Name,
    string Description
) : ICommand<Guid>;

public class CreateExpenseTypeValidator : AbstractValidator<CreateExpenseTypeCommand>
{
    public CreateExpenseTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class CreateExpenseTypeCommandHandler(DashboardDbContext db) : ICommandHandler<CreateExpenseTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateExpenseTypeCommand request, CancellationToken ct)
    {
        var nameExists = await db.ExpenseTypes
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Name == request.Name, ct);

        if (nameExists)
            throw new DuplicateDomainMemberException($"An expense type with the name '{request.Name}' already exists.");

        var expenseType = new ExpenseType
        {
            Name = request.Name,
            Description = request.Description,
        };

        db.ExpenseTypes.Add(expenseType);
        await db.SaveChangesAsync(ct);

        return expenseType.Id;
    }
}
