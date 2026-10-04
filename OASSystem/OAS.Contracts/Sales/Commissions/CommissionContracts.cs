namespace OAS.Contracts.Sales.Commissions;

public enum CommissionStatementStatus : byte { Draft = 1, Calculated = 2, Finalized = 3, Cancelled = 4 }

public sealed record CreateCommissionRuleRequest(string Code, string Name, Guid? EmployeeId, decimal RatePercent,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
public sealed record CommissionRuleDto(Guid Id, string Code, string Name, Guid? EmployeeId, string? EmployeeName,
    decimal RatePercent, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive, string RowVersion);
public sealed record CalculateCommissionStatementRequest(Guid EmployeeId, DateOnly FromDate, DateOnly ToDate);
public sealed record CommissionStatementActionRequest(string RowVersion);
public sealed record CommissionEntryDto(Guid Id, string SourceDocumentType, Guid SourceDocumentId, Guid SourceLineId,
    Guid SalesInvoiceId, Guid OriginalSalesInvoiceLineId, Guid? SalesReturnId, DateOnly SourceDate, decimal BaseSalesAmount,
    decimal RatePercent, decimal CommissionBaseAmount, Guid CommissionRuleId, string RuleCodeSnapshot,
    string RuleNameSnapshot, bool IsReversal);
public sealed record CommissionStatementDto(Guid Id, string StatementCode, Guid EmployeeId, string? EmployeeName,
    DateOnly FromDate, DateOnly ToDate, CommissionStatementStatus Status, decimal SalesBaseAmount,
    decimal ReturnsBaseAmount, decimal CommissionBaseAmount, DateTimeOffset? CalculatedAtUtc,
    DateTimeOffset? FinalizedAtUtc, string RowVersion, IReadOnlyList<CommissionEntryDto> Entries);
