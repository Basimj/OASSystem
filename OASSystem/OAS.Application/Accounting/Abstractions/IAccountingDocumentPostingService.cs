using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public interface IAccountingDocumentPostingService
{
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
