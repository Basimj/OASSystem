using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Queries.GetCashShiftMovements;

public sealed record GetCashShiftMovementsQuery(Guid ShiftId, int Page = 1) : IQuery<CashShiftMovementsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.View];
}

public sealed class GetCashShiftMovementsQueryHandler(ICashShiftMovementQueryService service)
    : IRequestHandler<GetCashShiftMovementsQuery, CashShiftMovementsDto>
{
    public Task<CashShiftMovementsDto> Handle(GetCashShiftMovementsQuery request, CancellationToken ct)
        => service.GetAsync(request.ShiftId, request.Page, ct);
}
