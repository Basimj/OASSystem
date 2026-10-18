using OAS.Contracts.Accounting.Reports;

namespace OAS.Contracts.Printing;

public sealed record CreateGeneralLedgerPrintJobRequest(
    GeneralLedgerFilterDto Filter,
    string WorkstationCode = "DEFAULT",
    string? TemplateCode = null,
    string? PrinterName = null,
    int Copies = 1,
    bool ShowPreview = false);

public sealed record CreateTrialBalancePrintJobRequest(
    TrialBalanceFilterDto Filter,
    string WorkstationCode = "DEFAULT",
    string? TemplateCode = null,
    string? PrinterName = null,
    int Copies = 1,
    bool ShowPreview = false);


public sealed record CreateSupplierStatementPrintJobRequest(
    Guid SupplierId, DateOnly FromDate, DateOnly ToDate,
    string WorkstationCode = "DEFAULT", string? TemplateCode = null,
    string? PrinterName = null, int Copies = 1, bool ShowPreview = true);
