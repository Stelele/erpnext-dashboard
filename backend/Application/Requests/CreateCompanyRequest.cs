namespace Application.Requests;

public record CreateCompanyRequest(
    Guid SiteId,
    string Name,
    string Description
);
