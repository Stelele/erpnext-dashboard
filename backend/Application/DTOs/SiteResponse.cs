using Domain.Sites;

namespace Application.DTOs;

public record SiteResponse(
    Guid Id,
    string Name,
    string Description,
    string Url,
    string ApiToken,
    List<Guid> Companies
)
{
    public static SiteResponse FromDomain(Site site) =>
        new(
            site.Id,
            site.Name,
            site.Description,
            site.Url,
            site.ApiToken,
            [.. site.Companies.Select(c => c.Id)]
        );
}
