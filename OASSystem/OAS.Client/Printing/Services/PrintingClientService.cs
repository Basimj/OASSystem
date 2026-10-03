using OAS.Client.Services.Http;
using OAS.Contracts.Printing;

namespace OAS.Client.Printing.Services;

public sealed class PrintingClientService(OasApiClient api) : IPrintingClientService
{
    private const string DefaultWorkstationCode = "DEFAULT";

    public Task<PrintJobCreatedDto?> PrintReceiptVoucherAsync(
        Guid voucherId,
        CancellationToken cancellationToken = default) =>
        QueueAsync(PrintDocumentTypes.ReceiptVoucher, voucherId, cancellationToken);

    public Task<PrintJobCreatedDto?> PrintPaymentVoucherAsync(
        Guid voucherId,
        CancellationToken cancellationToken = default) =>
        QueueAsync(PrintDocumentTypes.PaymentVoucher, voucherId, cancellationToken);

    public Task<PrintJobCreatedDto?> PrintSalesInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        QueueAsync(PrintDocumentTypes.SalesInvoice, invoiceId, cancellationToken);

    private Task<PrintJobCreatedDto?> QueueAsync(
        string documentType,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var request = new CreatePrintJobRequest(
            documentType,
            documentId,
            DefaultWorkstationCode,
            Copies: 1,
            ShowPreview: false);

        return api.PostAsync<CreatePrintJobRequest, PrintJobCreatedDto>(
            "api/printing/jobs",
            request,
            cancellationToken);
    }
}
