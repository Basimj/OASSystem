using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Reports;
namespace OAS.Application.Accounting.Reports.Queries.GetVoucherReport;
public sealed record GetVoucherReportQuery(VoucherReportFilter Filter) : IQuery<CashBankReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [AccountingPermissions.ReceiptVouchers.View, AccountingPermissions.PaymentVouchers.View, AccountingPermissions.Journals.View];
}
public sealed record ReverseVoucherCommand(bool Receipt, Guid Id, ReverseVoucherRequest Data) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [AccountingPermissions.Journals.Reverse, AccountingPermissions.Journals.Post, Receipt ? AccountingPermissions.ReceiptVouchers.Post : AccountingPermissions.PaymentVouchers.Post];
}
public sealed record PreviewVoucherReversalCommand(bool Receipt, Guid Id, ReverseVoucherRequest Data) : ICommand<VoucherReversalPreview>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [AccountingPermissions.Journals.Reverse, AccountingPermissions.Journals.Post, Receipt ? AccountingPermissions.ReceiptVouchers.Post : AccountingPermissions.PaymentVouchers.Post];
}
public sealed class VoucherReportHandler(IVoucherOperations service) : IRequestHandler<GetVoucherReportQuery, CashBankReportDto>, IRequestHandler<ReverseVoucherCommand, Guid>, IRequestHandler<PreviewVoucherReversalCommand, VoucherReversalPreview>
{
    public Task<CashBankReportDto> Handle(GetVoucherReportQuery request, CancellationToken ct) => service.ReportAsync(request.Filter, ct);
    public Task<VoucherReversalPreview> Handle(PreviewVoucherReversalCommand request, CancellationToken ct) => service.PreviewAsync(request.Receipt, request.Id, request.Data, ct);
    public Task<Guid> Handle(ReverseVoucherCommand request, CancellationToken ct) => service.ReverseAsync(request.Receipt, request.Id, request.Data, ct);
}
