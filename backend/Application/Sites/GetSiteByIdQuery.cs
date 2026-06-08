using Application.Abstractions;
using Application.DTOs;

namespace Application.Sites;

public record GetSiteByIdQuery(Guid Id) : IQuery<SiteResponse?>;
