using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Queries.GetCashShiftOpeningBalance;

public sealed record GetCashShiftOpeningBalanceQuery(Guid CashAccountId) : IQuery<CashShiftOpeningBalanceDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.View];
}

public sealed class GetCashShiftOpeningBalanceQueryHandler(ICashShiftOpeningBalanceService service)
    : IRequestHandler<GetCashShiftOpeningBalanceQuery, CashShiftOpeningBalanceDto>
{
    public async Task<CashShiftOpeningBalanceDto> Handle(GetCashShiftOpeningBalanceQuery request, CancellationToken ct)
        => new(await service.GetBookBalanceAsync(request.CashAccountId, ct));
}
