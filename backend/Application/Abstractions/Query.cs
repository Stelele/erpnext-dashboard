using MediatR;

namespace Application.Abstractions;

internal interface IQuery<TResponse> : IRequest<TResponse>;
internal interface IQueryHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IQuery<TResponse>;
