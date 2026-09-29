using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.SupplierCatalog;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.SupplierCatalog.Commands;

public sealed record CreateSupplierPriceCommand(Guid CatalogItemId, CreateSupplierPriceRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Catalog.Manage];
}

public sealed class CreateSupplierPriceCommandHandler(
    ISupplierCatalogRepository repository,
    IPurchasingReferenceDataPort references) : IRequestHandler<CreateSupplierPriceCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplierPriceCommand command, CancellationToken cancellationToken)
    {
        _ = await repository.GetByIdAsync(command.CatalogItemId, cancellationToken)
            ?? throw new NotFoundException("SupplierCatalogItem", command.CatalogItemId);
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(command.Request.CurrencyId, cancellationToken));

        if (command.Request.IsCurrent && await repository.HasCurrentPriceAsync(command.CatalogItemId, command.Request.CurrencyId, null, cancellationToken))
            throw new ConflictException("purchasing_supplier_price_current_duplicate", "يوجد سعر حالي فعال مسبقًا لنفس العملة. أغلق السعر الحالي قبل إضافة سعر جديد.");

        var price = SupplierPriceHistory.Create(Guid.NewGuid(), command.CatalogItemId, command.Request.CurrencyId,
            command.Request.UnitPrice, command.Request.EffectiveFrom, command.Request.EffectiveTo, command.Request.IsCurrent,
            command.Request.Notes);
        await repository.AddPriceAsync(price, cancellationToken);
        return price.Id;
    }
}
