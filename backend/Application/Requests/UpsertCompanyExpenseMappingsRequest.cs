using FluentValidation;

namespace Application.Requests;

public record UpsertCompanyExpenseMappingsRequest(
    List<MappingItemRequest> Mappings
);

public record MappingItemRequest(Guid ExpenseTypeId, string ErpnextAccountName);

public class UpsertCompanyExpenseMappingsRequestValidator : AbstractValidator<UpsertCompanyExpenseMappingsRequest>
{
    public UpsertCompanyExpenseMappingsRequestValidator()
    {
        RuleFor(x => x.Mappings).NotEmpty();
        RuleForEach(x => x.Mappings).ChildRules(mapping =>
        {
            mapping.RuleFor(x => x.ExpenseTypeId).NotEmpty();
            mapping.RuleFor(x => x.ErpnextAccountName).NotEmpty();
        });
    }
}
