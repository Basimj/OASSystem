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
}
