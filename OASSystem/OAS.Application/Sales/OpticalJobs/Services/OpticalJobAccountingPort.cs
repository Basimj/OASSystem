using OAS.Application.Accounting.Abstractions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.OpticalJobs.Services;

public sealed class OpticalJobAccountingPort(IProfileAccountingPostingService posting) : IOpticalJobAccountingPort
{
    private const string Module = "Optical";
    private const string DocumentType = "OpticalBreakage";
    private const string DamageRole = "ProductionDamageExpense";
    private const string InventoryRole = "Inventory";

    public Task<Guid> PostBreakageAsync(OpticalJobBreakage breakage, Guid warehouseId, decimal totalCostBase,
        Guid postedBy, DateTimeOffset postedAtUtc, CancellationToken cancellationToken = default)
    {
        if (totalCostBase <= 0m)
            throw new ArgumentOutOfRangeException(nameof(totalCostBase), "Breakage cost must be greater than zero.");

        var date = DateOnly.FromDateTime(postedAtUtc.UtcDateTime);
        IReadOnlyList<ProfileAccountingPostingLine> lines =
        [
            new(DocumentType, DamageRole, false, totalCostBase, 0m, false,
                $"Optical production damage / {breakage.ReasonCode}", ProductVariantId: breakage.ProductVariantId,
                WarehouseId: warehouseId, SourceLineId: breakage.OpticalJobLineId),
            new(DocumentType, InventoryRole, false, 0m, totalCostBase, false,
                $"Optical breakage inventory / {breakage.ReasonCode}", ProductVariantId: breakage.ProductVariantId,
                WarehouseId: warehouseId, SourceLineId: breakage.OpticalJobLineId)
        ];

        return posting.PostAsync(new ProfileAccountingPostingRequest(
            Module, DocumentType, breakage.Id, date, date,
            $"Optical breakage {breakage.Id:D}", null, 1m, lines),
            postedBy, postedAtUtc.UtcDateTime, cancellationToken);
    }
}
