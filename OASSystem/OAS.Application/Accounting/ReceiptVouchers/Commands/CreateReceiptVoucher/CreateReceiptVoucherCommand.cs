using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.ReceiptVouchers;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;

public sealed record CreateReceiptVoucherCommand(CreateReceiptVoucherRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ReceiptVouchers.Create];
}
