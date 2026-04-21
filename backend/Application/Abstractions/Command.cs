using MediatR;

namespace Application.Abstractions;

internal interface ICommand<TResponse> : IRequest<TResponse>;
internal interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>;
