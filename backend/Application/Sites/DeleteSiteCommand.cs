using Application.Abstractions;
using MediatR;

namespace Application.Sites;

public record DeleteSiteCommand(Guid Id) : ICommand<Unit>;
