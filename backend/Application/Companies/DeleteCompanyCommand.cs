using Application.Abstractions;
using MediatR;

namespace Application.Companies;

public record DeleteCompanyCommand(Guid Id) : ICommand<Unit>;
