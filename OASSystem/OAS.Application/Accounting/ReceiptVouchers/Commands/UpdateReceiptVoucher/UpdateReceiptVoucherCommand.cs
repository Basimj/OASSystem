using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.ReceiptVouchers;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.UpdateReceiptVoucher;

public sealed record UpdateReceiptVoucherCommand(Guid Id, UpdateReceiptVoucherRequest Data)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ReceiptVouchers.Edit];
}
