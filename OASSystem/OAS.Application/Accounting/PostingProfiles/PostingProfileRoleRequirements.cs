namespace OAS.Application.Accounting.PostingProfiles;

/// <summary>Minimal always-required roles used by existing posting services.
/// Conditional roles (tax, inventory movements, price variance) are validated
/// by their document-specific posting paths and readiness checks.</summary>
internal static class PostingProfileRoleRequirements
{
    public static IReadOnlyList<string> Required(string module, string documentType) =>
        (module.Trim().ToLowerInvariant(), documentType.Trim().ToLowerInvariant()) switch
        {
            ("sales", "salesinvoice") => ["SalesRevenue"],
            ("purchasing", "purchasereceipt") => ["Inventory", "GoodsReceivedNotInvoiced"],
            ("purchasing", "purchaseinvoice") => ["GoodsReceivedNotInvoiced"],
            ("accounting", "customeradvanceapplication") => ["CustomerAdvances"],
            ("optical", "opticalbreakage") => ["ProductionDamageExpense", "Inventory"],
            _ => []
        };

    public static bool HasRequiredRoles(string module, string documentType,
        IEnumerable<string> roles) =>
        Required(module, documentType).All(required => roles.Any(role =>
            string.Equals(role?.Trim(), required, StringComparison.OrdinalIgnoreCase)));
}
