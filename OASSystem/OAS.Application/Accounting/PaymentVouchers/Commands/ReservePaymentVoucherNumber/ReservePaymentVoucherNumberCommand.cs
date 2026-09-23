using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Common;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.ReservePaymentVoucherNumber;

public sealed record ReservePaymentVoucherNumberCommand(DateOnly VoucherDate) : ICommand<AccountingNumberReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.PaymentVouchers.Create];
}
