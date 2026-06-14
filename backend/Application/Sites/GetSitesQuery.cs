using Application.Abstractions;
using Application.Caching;
using Application.DTOs;

namespace Application.Sites;

[Cache(DurationMinutes = 10, KeyPrefix = "sites")]
public record GetSitesQuery(Guid[]? Sites) : IQuery<List<SiteResponse>>;
