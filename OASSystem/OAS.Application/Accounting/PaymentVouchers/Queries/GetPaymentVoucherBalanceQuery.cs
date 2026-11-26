using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentVouchers;

namespace OAS.Application.Accounting.PaymentVouchers.Queries;

public sealed record GetPaymentVoucherBalanceQuery(Guid DefinitionId, bool IsBank, DateOnly Date)
    : IQuery<PaymentVoucherBalanceDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.PaymentVouchers.View];
}

public sealed class GetPaymentVoucherBalanceQueryHandler(IPaymentVoucherBalanceQueryService service)
    : IRequestHandler<GetPaymentVoucherBalanceQuery, PaymentVoucherBalanceDto>
{
    public Task<PaymentVoucherBalanceDto> Handle(GetPaymentVoucherBalanceQuery request, CancellationToken ct) =>
        service.GetAsync(request.DefinitionId, request.IsBank, request.Date, ct);
}
