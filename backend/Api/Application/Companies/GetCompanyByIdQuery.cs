using Application.Abstractions;
using Application.DTOs;

namespace Application.Companies;

public record GetCompanyByIdQuery(Guid Id) : IQuery<ExtendedCompanyResponse?>;
