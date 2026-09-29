using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.SupplierCatalog;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.SupplierCatalog.Commands;

public sealed record CreateSupplierCatalogItemCommand(CreateSupplierCatalogItemRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Catalog.Manage];
}

public sealed class CreateSupplierCatalogItemCommandHandler(
    ISupplierCatalogRepository repository,
    IPurchasingReferenceDataPort references) : IRequestHandler<CreateSupplierCatalogItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplierCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(request.SupplierId, cancellationToken));
        PurchasingApplicationGuard.Product(await references.GetProductVariantAsync(request.ProductVariantId, cancellationToken));
        PurchasingApplicationGuard.Unit(await references.GetUnitAsync(request.PurchaseUnitId, cancellationToken));

        if (await repository.ExistsAsync(request.SupplierId, request.ProductVariantId, request.PurchaseUnitId, null, cancellationToken))
            throw new ConflictException("purchasing_supplier_catalog_duplicate", "هذا المنتج والوحدة مسجلان مسبقًا في كتالوج المورد.");

        var entity = SupplierCatalogItem.Create(Guid.NewGuid(), request.SupplierId, request.ProductVariantId,
            request.SupplierProductCode, request.SupplierProductName, request.PurchaseUnitId, request.UnitConversionFactor,
            request.LeadTimeDays, request.MinimumOrderQuantity, request.IsPreferred, request.IsActive);
        await repository.AddAsync(entity, cancellationToken);
        return entity.Id;
    }
}
