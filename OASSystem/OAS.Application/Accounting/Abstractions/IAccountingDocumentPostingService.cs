using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public interface IAccountingDocumentPostingService
{
    Task ValidateReceiptVoucherAsync(ReceiptVoucher voucher, IReadOnlyCollection<ReceiptVoucherLine> lines, CancellationToken ct = default);
    Task ValidatePaymentVoucherAsync(PaymentVoucher voucher, IReadOnlyCollection<PaymentVoucherLine> lines, CancellationToken ct = default);

    Task<Guid> PostReceiptVoucherAsync(
        ReceiptVoucher voucher,
        IReadOnlyCollection<ReceiptVoucherLine> lines,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);

    Task<Guid> PostPaymentVoucherAsync(
        PaymentVoucher voucher,
        IReadOnlyCollection<PaymentVoucherLine> lines,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);

    Task<Guid> PostExpenseAsync(
        Expense expense,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);
}
