using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Common;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.ReserveReceiptVoucherNumber;

public sealed record ReserveReceiptVoucherNumberCommand(DateOnly VoucherDate) : ICommand<AccountingNumberReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.ReceiptVouchers.Create];
}
