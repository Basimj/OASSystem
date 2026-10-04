namespace OAS.Contracts.Accounting.CustomerAdvances;

public sealed record CustomerAdvanceApplicationDto(
    Guid Id,
    Guid CustomerAdvanceId,
    Guid SalesInvoiceId,
    decimal Amount,
    decimal BaseAmount,
    decimal TargetBaseAmount,
    Guid JournalEntryId,
    DateTime AppliedAtUtc,
    string? AppliedBy,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy);
