using Domain.Abstractions;
using Domain.Companies;

namespace Domain.CompanySettings;

public class CompanySettings : Base
{
    public Guid CompanyId { get; set; }
    public string DefaultIncomeAccountName { get; set; } = "Sales";

    public Company Company { get; set; } = null!;
}
