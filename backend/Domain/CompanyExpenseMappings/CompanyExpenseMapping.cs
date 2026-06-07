using Domain.Abstractions;
using Domain.Companies;
using Domain.ExpenseTypes;

namespace Domain.CompanyExpenseMappings;

public class CompanyExpenseMapping : Base
{
    public Guid CompanyId { get; set; }
    public Guid ExpenseTypeId { get; set; }
    public string ErpnextAccountName { get; set; } = string.Empty;

    public Company Company { get; set; } = null!;
    public ExpenseType ExpenseType { get; set; } = null!;
}
