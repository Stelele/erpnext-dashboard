using Application.Abstractions;
using Application.DTOs;

namespace Application.Sites;

public record GetSitesQuery(Guid[]? Sites) : IQuery<List<SiteResponse>>;
