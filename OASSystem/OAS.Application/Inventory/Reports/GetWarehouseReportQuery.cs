using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Reports;

namespace OAS.Application.Inventory.Reports;

public sealed record GetWarehouseReportQuery(string Mode, DateOnly FromDate, DateOnly ToDate,
    Guid? WarehouseId, Guid? VariantId, Guid? ProductId, Guid? CategoryId, bool Current)
    : IQuery<WarehouseReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions =>
        [InventoryPermissions.Warehouses.View, InventoryPermissions.Products.View,
         InventoryPermissions.ProductVariants.View, InventoryPermissions.ProductCategories.View, InventoryPermissions.Units.View, Mode switch
         {
             "transfers" => InventoryPermissions.Transactions.View,
             "counts" => InventoryPermissions.StockCounts.View,
             "summary" or "details" when Current => InventoryPermissions.Balances.View,
             _ => InventoryPermissions.Ledger.View
         }];
}
public interface IWarehouseReportService
{
    Task<WarehouseReportDto> GetAsync(GetWarehouseReportQuery query, CancellationToken ct);
}
public sealed class GetWarehouseReportHandler(IWarehouseReportService service)
    : IRequestHandler<GetWarehouseReportQuery, WarehouseReportDto>
{
    public Task<WarehouseReportDto> Handle(GetWarehouseReportQuery request, CancellationToken ct) => service.GetAsync(request, ct);
}
