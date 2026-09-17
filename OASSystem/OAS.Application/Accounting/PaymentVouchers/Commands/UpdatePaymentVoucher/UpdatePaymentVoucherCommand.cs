using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentVouchers;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.UpdatePaymentVoucher;

public sealed record UpdatePaymentVoucherCommand(Guid Id, UpdatePaymentVoucherRequest Data)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentVouchers.Edit];
}
