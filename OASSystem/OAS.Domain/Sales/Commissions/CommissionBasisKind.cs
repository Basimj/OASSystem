namespace OAS.Domain.Sales.Commissions;

/// <summary>
/// Defines the monetary basis used when calculating a commission entry.
/// Persisted numeric values are stable and must not be reordered.
/// </summary>
public enum CommissionBasisKind : byte
{
    NetAfterDiscountExcludingTax = 1,
    GrossBeforeDiscount = 2,
    FinalIncludingTax = 3
}
