using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Commands.PostInventoryTransaction;

public sealed record PostInventoryTransactionCommand(
    Guid TransactionId,
    PostInventoryTransactionRequest? Request = null)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Transactions.Post];
}
