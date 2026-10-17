namespace OAS.Contracts.Accounting.Reports;

public sealed record SupplierAgingFilterDto(
    DateOnly AsOfDate,
    Guid? SupplierId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 100);

public sealed record SupplierAgingLineDto(
    string SourceType,
    Guid SourceId,
    string Reference,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    DateOnly DocumentDate,
    DateOnly PostingDate,
    DateOnly? DueDate,
    string DueDateProvenance,
    bool NeedsReview,
    int? DaysPastDue,
    string Bucket,
    decimal OriginalBaseAmount,
    decimal OutstandingBaseAmount,
    string? CurrencyCode,
    decimal? OriginalDocumentAmount);

public sealed record SupplierAgingSupplierSummaryDto(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    decimal NotDue,
    decimal Days0To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90,
    decimal Unknown,
    decimal TotalOutstanding,
    decimal UnappliedPayments,
    decimal UnappliedCredits);

public sealed record SupplierAgingReportDto(
    DateOnly AsOfDate,
    IReadOnlyList<SupplierAgingLineDto> Lines,
    IReadOnlyList<SupplierAgingSupplierSummaryDto> Suppliers,
    decimal NotDue,
    decimal Days0To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90,
    decimal Unknown,
    decimal TotalOutstanding,
    decimal UnappliedPayments,
    decimal UnappliedCredits,
    long TotalCount,
    int PageNumber,
    int PageSize);
