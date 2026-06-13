using Application.Abstractions;
using Application.Caching;
using MediatR;

namespace Application.Companies;

[InvalidateCache(Category = "company")]
[InvalidateCache(Category = "companies")]
[InvalidateCache(Category = "settings")]
[InvalidateCache(Category = "expense_mappings")]
public record DeleteCompanyCommand(Guid Id) : ICommand<Unit>;
