using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.SupplierCatalog;

namespace OAS.Application.Purchasing.SupplierCatalog.Commands;

public sealed record CloseSupplierPriceCommand(Guid CatalogItemId, Guid PriceId, CloseSupplierPriceRequest Request) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Catalog.Manage];
}

public sealed class CloseSupplierPriceCommandHandler(ISupplierCatalogRepository repository) : IRequestHandler<CloseSupplierPriceCommand>
{
    public async Task Handle(CloseSupplierPriceCommand command, CancellationToken cancellationToken)
    {
        var price = await repository.GetPriceForUpdateAsync(command.PriceId, cancellationToken)
            ?? throw new NotFoundException("SupplierPriceHistory", command.PriceId);
        if (price.SupplierCatalogItemId != command.CatalogItemId)
            throw new NotFoundException("SupplierPriceHistory", command.PriceId);
        PurchasingRowVersion.EnsureMatches(price.RowVersion, command.Request.RowVersion, "Supplier price");
        price.Close(command.Request.EffectiveTo);
        repository.UpdatePrice(price);
    }
}
