using Domain.ExpenseTypes;

namespace Application.DTOs;

public record ExpenseTypeResponse(Guid Id, string Name, string Description)
{
    public static ExpenseTypeResponse FromDomain(ExpenseType type) =>
        new(type.Id, type.Name, type.Description);
}
