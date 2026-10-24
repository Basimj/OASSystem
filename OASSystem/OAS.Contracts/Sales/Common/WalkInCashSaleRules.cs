using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Common;

/// <summary>Never permit debt to accumulate under the shared walk-in customer.</summary>
public static class WalkInCashSaleRules
{
    public static bool RequiresFullPayment(Guid customerId, Guid? walkInCustomerId, SalesPaymentPlan plan)
        => walkInCustomerId is { } id && id == customerId && plan != SalesPaymentPlan.FullNow;
}
