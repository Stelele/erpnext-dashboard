using Application.Abstractions;
using Application.DTOs;

namespace Application.Companies;

public record GetCompaniesQuery(Guid[]? CompanyIds) : IQuery<List<CompanyResponse>>;
