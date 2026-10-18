using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Printing;

/// <summary>
/// Immutable snapshot for the desktop template. Monetary fields come directly from
/// the authorized SupplierStatementDto, with no client-side balance re-calculation.
/// </summary>
public sealed record SupplierStatementPrintLine(
    string PostingDate, string DocumentDate, string JournalNumber,
    string DocumentType, string DocumentReference, string Description,
    decimal Debit, decimal Credit, decimal RunningBalance,
    string TransactionCurrencyCode, decimal? TransactionDebit, decimal? TransactionCredit);

public sealed record SupplierStatementPrintPayload(
    string DocumentTitleAr, string DocumentTitleEn, string ReportNumber,
    string SupplierCode, string SupplierName, string AccountCode, string AccountName,
    string FromDate, string ToDate, string BaseCurrencyCode,
    decimal OpeningBalance, decimal TotalDebit, decimal TotalCredit, decimal ClosingBalance,
    int LineCount, string PrintedAt, string RequestedBy, object Company,
    IReadOnlyList<SupplierStatementPrintLine> Lines, string SchemaVersion)
{
    public static SupplierStatementPrintPayload Create(SupplierStatementDto report, object company,
        string baseCurrency, string? requestedBy, DateTimeOffset time) => new(
            "كشف حساب المورد", "SUPPLIER STATEMENT",
            $"SUP-{report.SupplierCode}-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}",
            report.SupplierCode, report.SupplierName, report.AccountCode, report.AccountName,
            report.FromDate.ToString("dd/MM/yyyy"), report.ToDate.ToString("dd/MM/yyyy"),
            baseCurrency,
            report.OpeningLiabilityBaseAmount,
            report.PeriodDebitBaseAmount,
            report.PeriodCreditBaseAmount,
            report.ClosingLiabilityBaseAmount,
            report.Movements.Count,
            time.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture),
            requestedBy ?? string.Empty, company,
            report.Movements.Select(x => new SupplierStatementPrintLine(
                x.PostingDate.ToString("dd/MM/yyyy"), x.DocumentDate.ToString("dd/MM/yyyy"),
                x.JournalNumber, x.SourceDocumentType ?? "", x.Reference, x.Description,
                x.DebitBaseAmount, x.CreditBaseAmount, x.RunningLiabilityBaseAmount,
                x.TransactionCurrencyCode ?? "", x.TransactionDebitAmount, x.TransactionCreditAmount)).ToArray(),
            "AP-08/v1");
}
