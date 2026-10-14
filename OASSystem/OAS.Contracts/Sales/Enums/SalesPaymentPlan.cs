namespace OAS.Contracts.Sales.Enums;

public enum SalesPaymentPlan : byte
{
    FullNow = 1,
    PartialNow = 2,
    // Value 3 was previously PayOnPickup. Do not reuse it; persisted enum values must remain stable.
    AccountCredit = 4
}
