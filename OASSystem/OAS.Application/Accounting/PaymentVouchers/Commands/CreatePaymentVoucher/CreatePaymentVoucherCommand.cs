using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentVouchers;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;

public sealed record CreatePaymentVoucherCommand(CreatePaymentVoucherRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentVouchers.Create];
}
