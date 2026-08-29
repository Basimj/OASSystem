using MediatR;

namespace OAS.Application.Abstractions.Messaging;

public interface ICommandBase { }
public interface INonTransactionalCommandBase { }
public interface ICommand : IRequest, ICommandBase { }
public interface ICommand<out TResponse> : IRequest<TResponse>, ICommandBase { }
public interface INonTransactionalCommand<out TResponse> : ICommand<TResponse>, INonTransactionalCommandBase { }
