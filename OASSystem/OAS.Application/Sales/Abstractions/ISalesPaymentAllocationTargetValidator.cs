using OAS.Application.Accounting.Abstractions;

namespace OAS.Application.Sales.Abstractions;

[Obsolete("Use IPaymentAllocationTargetValidator. This compatibility interface is retained for source compatibility.")]
public interface ISalesPaymentAllocationTargetValidator : IPaymentAllocationTargetValidator
{
}
