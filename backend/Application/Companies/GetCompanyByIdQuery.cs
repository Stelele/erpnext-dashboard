using Application.Abstractions;
using Application.Caching;
using Application.DTOs;

namespace Application.Companies;

[Cache(DurationMinutes = 5, KeyPrefix = "company")]
public record GetCompanyByIdQuery(Guid Id) : IQuery<CompanyResponse?>;
