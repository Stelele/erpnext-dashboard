namespace Application.Requests;

public record UpsertCompanyExpenseMappingsRequest(
    List<MappingItemRequest> Mappings
);

public record MappingItemRequest(Guid ExpenseTypeId, string ErpnextAccountName);
