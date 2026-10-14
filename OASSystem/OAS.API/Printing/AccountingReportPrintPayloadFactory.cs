using MediatR;
using OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;
using OAS.Application.Accounting.Reports.Queries.GetTrialBalance;
using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Printing;

public sealed class AccountingReportPrintPayloadFactory(
    ISender sender,
    IConfiguration configuration)
{
    public async Task<object> BuildGeneralLedgerAsync(
        GeneralLedgerFilterDto filter,
        string? requestedBy,
        CancellationToken cancellationToken)
    {
        var report = await sender.Send(
            new GetGeneralLedgerQuery(
                filter.FromDate,
                filter.ToDate,
                filter.AccountId,
                filter.FiscalYearId,
                filter.FiscalPeriodId,
                filter.IncludeAccountSubtree,
                filter.JournalType,
                filter.SourceModule,
                filter.CustomerId,
                filter.SupplierId,
                filter.Reference,
                filter.IncludeOpeningBalance),
            cancellationToken);

        var lines = new List<object>();
        foreach (var account in report.Accounts.OrderBy(x => x.AccountCode, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add(new
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountNameAr,
                PostingDate = string.Empty,
                JournalNumber = string.Empty,
                Description = "الرصيد الافتتاحي",
                Reference = string.Empty,
                Debit = 0m,
                Credit = 0m,
                RunningBalance = account.OpeningBalance
            });

            foreach (var line in account.Lines.OrderBy(x => x.PostingDate).ThenBy(x => x.JournalNumber).ThenBy(x => x.LineNumber))
            {
                lines.Add(new
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountNameAr,
                    PostingDate = line.PostingDate.ToString("dd/MM/yyyy"),
                    line.JournalNumber,
                    line.Description,
                    Reference = BuildReference(line),
                    line.Debit,
                    line.Credit,
                    line.RunningBalance
                });
            }

            lines.Add(new
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountNameAr,
                PostingDate = string.Empty,
                JournalNumber = string.Empty,
                Description = "إجمالي الحساب / الرصيد الختامي",
                Reference = string.Empty,
                Debit = account.PeriodDebit,
                Credit = account.PeriodCredit,
                RunningBalance = account.ClosingBalance
            });
        }

        var selectedAccount = filter.AccountId.HasValue
            ? report.Accounts.FirstOrDefault(x => x.AccountId == filter.AccountId.Value)
            : null;

        return new
        {
            DocumentTitleAr = "دفتر الأستاذ العام",
            DocumentTitleEn = "GENERAL LEDGER",
            ReportNumber = $"GL-{filter.FromDate:yyyyMMdd}-{filter.ToDate:yyyyMMdd}",
            FromDate = filter.FromDate.ToString("dd/MM/yyyy"),
            ToDate = filter.ToDate.ToString("dd/MM/yyyy"),
            AccountCode = selectedAccount?.AccountCode ?? string.Empty,
            AccountName = selectedAccount?.AccountNameAr ?? (filter.AccountId.HasValue ? "حساب محدد" : "جميع الحسابات"),
            ScopeText = selectedAccount is null
                ? filter.AccountId.HasValue ? "الحساب المحدد" : "جميع الحسابات"
                : $"{selectedAccount.AccountCode} - {selectedAccount.AccountNameAr}",
            AccountCount = report.Accounts.Count,
            OpeningBalanceDisplay = selectedAccount is null ? "—" : selectedAccount.OpeningBalance.ToString("N2"),
            report.TotalDebit,
            report.TotalCredit,
            ClosingBalanceDisplay = selectedAccount is null ? "—" : selectedAccount.ClosingBalance.ToString("N2"),
            RequestedBy = requestedBy ?? string.Empty,
            PrintedAt = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"),
            Company = Company(),
            Lines = lines
        };
    }

    public async Task<object> BuildTrialBalanceAsync(
        TrialBalanceFilterDto filter,
        string? requestedBy,
        CancellationToken cancellationToken)
    {
        var report = await sender.Send(
            new GetTrialBalanceQuery(
                filter.FromDate,
                filter.ToDate,
                filter.AsOfDate,
                filter.FiscalYearId,
                filter.FiscalPeriodId,
                filter.AccountLevel,
                filter.PostingAccountsOnly,
                filter.IncludeZeroBalance,
                filter.GroupedTree),
            cancellationToken);

        return new
        {
            DocumentTitleAr = "ميزان المراجعة",
            DocumentTitleEn = "TRIAL BALANCE",
            ReportNumber = $"TB-{filter.FromDate:yyyyMMdd}-{filter.ToDate:yyyyMMdd}",
            FromDate = report.FromDate.ToString("dd/MM/yyyy"),
            ToDate = report.ToDate.ToString("dd/MM/yyyy"),
            AsOfDate = filter.AsOfDate?.ToString("dd/MM/yyyy") ?? string.Empty,
            report.OpeningDebitTotal,
            report.OpeningCreditTotal,
            report.PeriodDebitTotal,
            report.PeriodCreditTotal,
            report.ClosingDebitTotal,
            report.ClosingCreditTotal,
            report.IsBalanced,
            BalanceStatus = report.IsBalanced ? "متوازن" : "غير متوازن",
            RequestedBy = requestedBy ?? string.Empty,
            PrintedAt = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"),
            Company = Company(),
            Lines = report.Rows
                .OrderBy(x => x.AccountCode, StringComparer.OrdinalIgnoreCase)
                .Select(x => new
                {
                    x.AccountCode,
                    AccountName = x.AccountNameAr,
                    x.OpeningDebit,
                    x.OpeningCredit,
                    x.PeriodDebit,
                    x.PeriodCredit,
                    x.ClosingDebit,
                    x.ClosingCredit
                })
                .ToArray()
        };
    }

    private static string BuildReference(GeneralLedgerLineDto line)
    {
        if (!string.IsNullOrWhiteSpace(line.Reference))
            return line.Reference!;

        if (!string.IsNullOrWhiteSpace(line.SourceDocumentType))
            return string.IsNullOrWhiteSpace(line.SourceModule)
                ? line.SourceDocumentType!
                : $"{line.SourceModule}/{line.SourceDocumentType}";

        return line.SourceModule ?? string.Empty;
    }

    private object Company() => new
    {
        Name = configuration["Company:Name"] ?? configuration["Printing:Company:Name"] ?? "OAS",
        Address = configuration["Company:Address"] ?? configuration["Printing:Company:Address"] ?? string.Empty,
        Phone = configuration["Company:Phone"] ?? configuration["Printing:Company:Phone"] ?? string.Empty,
        CommercialRegistration = configuration["Company:CommercialRegistration"] ?? configuration["Printing:Company:CommercialRegistration"] ?? string.Empty,
        TaxNumber = configuration["Company:TaxNumber"] ?? configuration["Printing:Company:TaxNumber"] ?? string.Empty
    };
}
