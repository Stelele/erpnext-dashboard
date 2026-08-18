namespace Application.Requests;

public record CreateSiteRequest(
    string Name,
    string Url,
    string Description,
    string ApiToken
);
