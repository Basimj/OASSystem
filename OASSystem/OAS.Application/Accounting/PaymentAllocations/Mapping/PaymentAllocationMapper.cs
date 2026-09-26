using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Domain.Accounting.Entities;
using ContractAllocationTargetDocumentType = OAS.Contracts.Accounting.Enums.AllocationTargetDocumentType;
using ContractPaymentSourceType = OAS.Contracts.Accounting.Enums.PaymentSourceType;
namespace OAS.Application.Accounting.PaymentAllocations.Mapping;
public sealed class PaymentAllocationMapper
{
    public PaymentAllocationDto ToRead(PaymentAllocation source) => new(
        source.Id, source.ReceiptVoucherLineId, source.PaymentVoucherLineId,
        (ContractPaymentSourceType)(int)source.PaymentSourceType, source.PaymentSourceId,
        (ContractAllocationTargetDocumentType)(int)source.TargetDocumentType, source.TargetDocumentId,
        source.CurrencyId, source.CurrencyCodeSnapshot, source.AllocatedAmount, source.ExchangeRate, source.BaseAllocatedAmount,
        source.AllocatedAtUtc, source.CreatedBy);
}
