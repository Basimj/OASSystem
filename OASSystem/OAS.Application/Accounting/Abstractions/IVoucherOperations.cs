using OAS.Contracts.Accounting.Reports;
namespace OAS.Application.Accounting.Abstractions;
public interface IVoucherOperations
{
    Task<CashBankReportDto> ReportAsync(VoucherReportFilter filter, CancellationToken ct);
    Task<VoucherReversalPreview> PreviewAsync(bool receipt, Guid id, ReverseVoucherRequest request, CancellationToken ct);
    Task<Guid> ReverseAsync(bool receipt, Guid id, ReverseVoucherRequest request, CancellationToken ct);
}
