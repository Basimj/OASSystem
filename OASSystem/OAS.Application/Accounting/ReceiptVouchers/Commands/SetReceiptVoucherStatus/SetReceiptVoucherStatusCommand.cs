using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.ReceiptVouchers;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;

public sealed record SetReceiptVoucherStatusCommand(Guid Id, SetReceiptVoucherStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ReceiptVouchers.Edit];
}
