using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Accounting.Entities;
using OAS.Application.Sales.Returns.Services;
using OAS.Contracts.Sales.Returns;
namespace OAS.Application.Sales.Returns.Queries;
public sealed class GetCustomerCreditBalanceQueryHandler(CustomerCreditLedgerService ledger)
    : IRequestHandler<GetCustomerCreditBalanceQuery,CustomerCreditBalanceDto>
{
    public Task<CustomerCreditBalanceDto> Handle(GetCustomerCreditBalanceQuery request,CancellationToken ct)
        => ledger.GetAsync(request.CustomerId,ct);
}


/// <summary>204 when no committed operation exists; does not create a voucher or application.</summary>
public sealed class GetCustomerCreditOperationQueryHandler(
    IReadRepository<CustomerCreditApplication, Guid> credits,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
    IReadRepository<PaymentVoucher, Guid> vouchers)
    : IRequestHandler<GetCustomerCreditOperationQuery, CustomerCreditSettlementDto?>
{
    public async Task<CustomerCreditSettlementDto?> Handle(
        GetCustomerCreditOperationQuery request, CancellationToken ct)
    {
        if (request.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            request.IdempotencyKey.Length > 80)
            throw new ConflictException("customer_credit_operation_key_invalid",
                "معرف التسوية المطلوب غير صالح.");

        var rows = await credits.ListAsync(new Specification<CustomerCreditApplication>()
            .Where(x => x.CustomerId == request.CustomerId &&
                x.IdempotencyKey == request.IdempotencyKey).ApplyPaging(0, 1), ct);
        if (rows.Count == 0) return null;
        var result = CustomerCreditLedgerService.Map(rows[0]);
        if (result.RefundPaymentVoucherLineId is Guid lineId)
        {
            var line = await paymentLines.GetByIdAsync(lineId, ct);
            if (line is not null)
            {
                var voucher = await vouchers.GetByIdAsync(line.PaymentVoucherId, ct);
                result = result with { PaymentVoucherNumber = voucher?.VoucherNumber };
            }
        }
        return result;
    }
}
