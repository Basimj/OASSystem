using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Commissions;

public enum CommissionStatementStatus : byte
{
    Draft = 1,
    Calculated = 2,
    Finalized = 3,
    Cancelled = 4,
    Reviewed = 5,
    Approved = 6,
    Settled = 7
}

public enum CommissionBasisKind : byte
{
    NetAfterDiscountExcludingTax = 1,
    GrossBeforeDiscount = 2,
    FinalIncludingTax = 3
}

public sealed record CreateCommissionRuleRequest(
    string Code,
    string Name,
    Guid? EmployeeId,
    decimal RatePercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    Guid? ProductVariantId = null,
    Guid? ProductCategoryId = null,
    SalesLineType? SaleType = null,
    int Priority = 0,
    CommissionBasisKind BasisKind = CommissionBasisKind.NetAfterDiscountExcludingTax);

public sealed record CommissionRuleDto(
    Guid Id,
    string Code,
    string Name,
    Guid? EmployeeId,
    string? EmployeeName,
    decimal RatePercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string RowVersion,
    Guid? ProductVariantId = null,
    Guid? ProductCategoryId = null,
    SalesLineType? SaleType = null,
    int Priority = 0,
    CommissionBasisKind BasisKind = CommissionBasisKind.NetAfterDiscountExcludingTax);

public sealed record CalculateCommissionStatementRequest(Guid EmployeeId, DateOnly FromDate, DateOnly ToDate);
public sealed record CommissionStatementActionRequest(string RowVersion);

public sealed record CommissionEntryDto(
    Guid Id,
    string SourceDocumentType,
    Guid SourceDocumentId,
    Guid SourceLineId,
    Guid SalesInvoiceId,
    Guid OriginalSalesInvoiceLineId,
    Guid? SalesReturnId,
    DateOnly SourceDate,
    decimal BaseSalesAmount,
    decimal RatePercent,
    decimal CommissionBaseAmount,
    Guid CommissionRuleId,
    string RuleCodeSnapshot,
    string RuleNameSnapshot,
    bool IsReversal,
    CommissionBasisKind BasisKindSnapshot = CommissionBasisKind.NetAfterDiscountExcludingTax);

public sealed record CommissionStatementDto(
    Guid Id,
    string StatementCode,
    Guid EmployeeId,
    string? EmployeeName,
    DateOnly FromDate,
    DateOnly ToDate,
    CommissionStatementStatus Status,
    decimal SalesBaseAmount,
    decimal ReturnsBaseAmount,
    decimal CommissionBaseAmount,
    DateTimeOffset? CalculatedAtUtc,
    DateTimeOffset? FinalizedAtUtc,
    string RowVersion,
    IReadOnlyList<CommissionEntryDto> Entries,
    DateTimeOffset? ReviewedAtUtc = null,
    DateTimeOffset? ApprovedAtUtc = null,
    DateTimeOffset? SettledAtUtc = null);
