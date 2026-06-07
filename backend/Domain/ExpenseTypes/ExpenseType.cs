using Domain.Abstractions;

namespace Domain.ExpenseTypes;

public class ExpenseType : Base
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
