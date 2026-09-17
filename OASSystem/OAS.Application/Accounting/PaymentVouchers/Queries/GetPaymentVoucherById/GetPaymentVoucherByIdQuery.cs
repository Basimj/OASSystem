using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentVouchers;

namespace OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;

public sealed record GetPaymentVoucherByIdQuery(Guid Id)
    : IQuery<PaymentVoucherDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentVouchers.View];
}
