namespace OAS.Domain.Accounting;

/// <summary>
/// Stable semantic roles persisted on journal lines. These values are part of the
/// accounting lineage and must not be localized or inferred from Description.
/// </summary>
public static class AccountingPostingRoles
{
    public const string SalesCustomerReceivable = "SalesCustomerReceivable";
    public const string SalesRevenue = "SalesRevenue";
    public const string SalesTax = "SalesTax";
    public const string SalesInventory = "SalesInventory";
    public const string SalesCOGS = "SalesCOGS";
}
