using Application.Abstractions;
using Application.DTOs;

namespace Application.Sites;

public record GetCompanyLogoQuery(Guid SiteId, string Company) : IQuery<LogoResponse?>;
