using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Inventory.Reports;

namespace OAS.Application.Inventory.Reports;

public sealed record GetSupplierProductMovementReportQuery(DateOnly FromDate, DateOnly ToDate,
    Guid? VariantId, Guid? ProductId, Guid? CategoryId, Guid? WarehouseId)
    : IQuery<SupplierProductMovementReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Ledger.View, InventoryPermissions.Products.View,
         InventoryPermissions.ProductVariants.View, AccountingPermissions.Suppliers.View, PurchasingPermissions.View];
}
public interface ISupplierProductMovementReportService
{
    Task<SupplierProductMovementReportDto> GetAsync(GetSupplierProductMovementReportQuery query, CancellationToken ct);
}
public sealed class GetSupplierProductMovementReportHandler(ISupplierProductMovementReportService service)
    : IRequestHandler<GetSupplierProductMovementReportQuery, SupplierProductMovementReportDto>
{
    public Task<SupplierProductMovementReportDto> Handle(GetSupplierProductMovementReportQuery request, CancellationToken ct)
        => service.GetAsync(request, ct);
}
