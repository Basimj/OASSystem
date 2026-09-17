using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.ReceiptVouchers;

namespace OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;

public sealed record GetReceiptVoucherByIdQuery(Guid Id)
    : IQuery<ReceiptVoucherDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ReceiptVouchers.View];
}
