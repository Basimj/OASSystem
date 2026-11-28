using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;

namespace OAS.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICommandBase || request is INonTransactionalCommandBase)
            return next(cancellationToken);

        if (request is OAS.Application.Inventory.Transactions.Openings.SaveInventoryOpening or OAS.Application.Inventory.Transactions.Openings.PostInventoryOpening or OAS.Application.Inventory.Transactions.Openings.DeleteInventoryOpening or OAS.Application.Inventory.Transactions.Openings.ReverseInventoryOpening)
            return unitOfWork.ExecuteSerializableAsync(ct => next(ct), cancellationToken);

        return unitOfWork.ExecuteInTransactionAsync(
            transactionCancellationToken => next(transactionCancellationToken),
            cancellationToken);
    }
}
