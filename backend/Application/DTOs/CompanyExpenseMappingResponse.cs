using Domain.CompanyExpenseMappings;

namespace Application.DTOs;

public record CompanyExpenseMappingResponse(
    Guid Id,
    Guid ExpenseTypeId,
    string ExpenseTypeName,
    string ErpnextAccountName
)
{
    public static CompanyExpenseMappingResponse FromDomain(CompanyExpenseMapping mapping, string expenseTypeName) =>
        new(mapping.Id, mapping.ExpenseTypeId, expenseTypeName, mapping.ErpnextAccountName);
}
