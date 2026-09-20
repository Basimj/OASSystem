using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;

public sealed record CreateInventoryTransactionCommand(CreateInventoryTransactionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Transactions.Create];
}
