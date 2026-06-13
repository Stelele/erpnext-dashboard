using Application.Abstractions;
using Application.Caching;
using Application.DTOs;

namespace Application.Companies;

[Cache(DurationMinutes = 5, KeyPrefix = "companies")]
public record GetCompaniesQuery(Guid[]? CompanyIds) : IQuery<List<CompanyResponse>>;
