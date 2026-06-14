using Application.Abstractions;
using Application.Caching;
using MediatR;

namespace Application.Sites;

[InvalidateCache(Category = "sites")]
[InvalidateCache(Category = "company")]
[InvalidateCache(Category = "companies")]
public record DeleteSiteCommand(Guid Id) : ICommand<Unit>;
