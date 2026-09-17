using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Queries.GetCashShiftById;

public sealed record GetCashShiftByIdQuery(Guid Id)
    : IQuery<CashShiftDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashShifts.View];
}
