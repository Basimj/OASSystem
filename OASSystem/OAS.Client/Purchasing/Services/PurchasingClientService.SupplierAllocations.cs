using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Purchasing.SupplierAllocations;

namespace OAS.Client.Purchasing.Services;

public sealed partial class PurchasingClientService
{
    public Task<SupplierAllocationCenterDto?> GetSupplierAllocationCenterAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
        => _api.GetAsync<SupplierAllocationCenterDto>(
            $"api/purchasing/supplier-allocations/{supplierId:D}",
            cancellationToken);

    public Task<PaymentAllocationDto?> CreateSupplierPaymentAllocationAsync(
        CreatePaymentAllocationRequest request,
        CancellationToken cancellationToken = default)
        => _api.PostAsync<CreatePaymentAllocationRequest, PaymentAllocationDto>(
            "api/accounting/payment-allocations",
            request,
            cancellationToken);

    public Task<ReversePaymentAllocationResultDto?> ReverseSupplierPaymentAllocationAsync(
        Guid allocationId,
        ReversePaymentAllocationRequest request,
        CancellationToken cancellationToken = default)
        => _api.PostAsync<ReversePaymentAllocationRequest, ReversePaymentAllocationResultDto>(
            $"api/accounting/payment-allocations/{allocationId:D}/reverse",
            request,
            cancellationToken);
}
