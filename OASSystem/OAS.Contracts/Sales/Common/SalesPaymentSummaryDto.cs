namespace OAS.Contracts.Sales.Common;

public sealed record SalesPaymentSummaryDto(
    decimal TotalAmount,
    decimal PaidNowAmount,
    decimal ExistingAdvanceBalance,
    decimal AppliedAdvanceAmount,
    decimal PaidAmount,
    decimal OutstandingAmount);
