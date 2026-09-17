using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.CashShifts.Queries.GetCashShifts;

public sealed record GetCashShiftsQuery(PageRequest Request)
    : IQuery<PagedResult<CashShiftDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashShifts.View];
}
