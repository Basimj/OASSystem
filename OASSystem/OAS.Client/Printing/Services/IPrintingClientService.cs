using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Printing;

namespace OAS.Client.Printing.Services;

public interface IPrintingClientService
{
    Task<PrintJobCreatedDto?> PrintReceiptVoucherAsync(
        Guid voucherId,
        CancellationToken cancellationToken = default);

    Task<PrintJobCreatedDto?> PrintPaymentVoucherAsync(
        Guid voucherId,
        CancellationToken cancellationToken = default);

    Task<PrintJobCreatedDto?> PrintPayslipAsync(
        Guid employeePayrollId,
        CancellationToken cancellationToken = default);

    Task<PrintJobCreatedDto?> PrintSalesInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<PrintJobCreatedDto?> PrintPurchaseInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<PrintJobCreatedDto?> PrintGeneralLedgerAsync(
        GeneralLedgerFilterDto filter,
        bool showPreview = false,
        CancellationToken cancellationToken = default);

    Task<PrintJobCreatedDto?> PrintTrialBalanceAsync(
        TrialBalanceFilterDto filter,
        bool showPreview = false,
        CancellationToken cancellationToken = default);
}
