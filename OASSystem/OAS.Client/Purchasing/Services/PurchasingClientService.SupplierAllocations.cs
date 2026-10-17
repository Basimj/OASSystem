using OAS.Contracts.Purchasing.SupplierAllocations;

namespace OAS.Client.Purchasing.Services;

public sealed partial class PurchasingClientService
{
    public Task<SupplierAllocationCenterDto?> GetSupplierAllocationCenterAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
        => api.GetAsync<SupplierAllocationCenterDto>(
            $"api/purchasing/supplier-allocations/{supplierId:D}",
            cancellationToken);
}
