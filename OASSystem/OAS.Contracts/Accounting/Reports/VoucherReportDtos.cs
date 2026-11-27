namespace OAS.Contracts.Accounting.Reports;

public sealed record VoucherReportFilter(DateOnly FromDate, DateOnly ToDate, string Mode = "all", string Status = "posted", string Kind = "all", Guid? CurrencyId = null, Guid? AccountId = null, int? Method = null, int? PartyType = null, string? Party = null, Guid? PostedBy = null, Guid? CashAccountId = null, Guid? BankAccountId = null);
public sealed record ReverseVoucherRequest(DateOnly Date, string Reason);

public sealed record VoucherReversalPreview(bool CanReverse, string? Error, int LineCount, int AllocationCount, Guid? JournalId);
