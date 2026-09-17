using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentVouchers;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;

public sealed record SetPaymentVoucherStatusCommand(Guid Id, SetPaymentVoucherStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentVouchers.Edit];
}
