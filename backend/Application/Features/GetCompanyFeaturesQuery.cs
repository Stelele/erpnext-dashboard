using Application.Abstractions;
using Application.Users;
using Domain.Exceptions;
using Infrastructure.Models;

namespace Application.Features;

public record GetCompanyFeaturesQuery(Guid CompanyId) : IQuery<List<string>>;

public class GetCompanyFeaturesQueryHandler(DashboardDbContext db, IUserContext userContext) : IQueryHandler<GetCompanyFeaturesQuery, List<string>>
{
    public Task<List<string>> Handle(GetCompanyFeaturesQuery request, CancellationToken ct)
    {
        if (!userContext.IsAdmin && !userContext.CompanyIds.Contains(request.CompanyId))
            throw new NotFoundException($"Company not found: {request.CompanyId}");

        return CompanyFeaturesResolver.GetEffectiveCodesAsync(db, request.CompanyId, ct);
    }
}
