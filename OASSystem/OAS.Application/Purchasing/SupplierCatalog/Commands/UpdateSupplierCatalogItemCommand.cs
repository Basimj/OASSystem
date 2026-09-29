using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.SupplierCatalog;

namespace OAS.Application.Purchasing.SupplierCatalog.Commands;

public sealed record UpdateSupplierCatalogItemCommand(Guid Id, UpdateSupplierCatalogItemRequest Request) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Catalog.Manage];
}

public sealed class UpdateSupplierCatalogItemCommandHandler(
    ISupplierCatalogRepository repository,
    IPurchasingReferenceDataPort references) : IRequestHandler<UpdateSupplierCatalogItemCommand>
{
    public async Task Handle(UpdateSupplierCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("SupplierCatalogItem", command.Id);
        PurchasingRowVersion.EnsureMatches(entity.RowVersion, command.Request.RowVersion, "Supplier catalog item");
        PurchasingApplicationGuard.Unit(await references.GetUnitAsync(command.Request.PurchaseUnitId, cancellationToken));

        if (await repository.ExistsAsync(entity.SupplierId, entity.ProductVariantId, command.Request.PurchaseUnitId, entity.Id, cancellationToken))
            throw new ConflictException("purchasing_supplier_catalog_duplicate", "هذا المنتج والوحدة مسجلان مسبقًا في كتالوج المورد.");

        entity.Update(command.Request.SupplierProductCode, command.Request.SupplierProductName, command.Request.PurchaseUnitId,
            command.Request.UnitConversionFactor, command.Request.LeadTimeDays, command.Request.MinimumOrderQuantity,
            command.Request.IsPreferred, command.Request.IsActive);
        repository.Update(entity);
    }
}
