using MediatR;

namespace OAS.Application.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse> { }
