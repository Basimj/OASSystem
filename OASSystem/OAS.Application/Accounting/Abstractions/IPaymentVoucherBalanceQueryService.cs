using OAS.Contracts.Accounting.PaymentVouchers;

namespace OAS.Application.Accounting.Abstractions;

public interface IPaymentVoucherBalanceQueryService
{
    Task<PaymentVoucherBalanceDto> GetAsync(Guid definitionId, bool isBank, DateOnly date, CancellationToken ct);
}
