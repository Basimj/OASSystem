using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
namespace OAS.Application.Accounting.CashShifts;
public sealed record GetCashShiftClosingPreviewQuery(Guid Id) : IQuery<decimal>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.View];
}
public sealed class GetCashShiftClosingPreviewQueryHandler(IReadRepository<CashShift,Guid> shifts, ICashShiftBalanceService balance, TimeProvider clock) : IRequestHandler<GetCashShiftClosingPreviewQuery,decimal>
{
    public async Task<decimal> Handle(GetCashShiftClosingPreviewQuery request,CancellationToken ct)
    {
        var shift=await shifts.GetByIdAsync(request.Id,ct) ?? throw new NotFoundException(nameof(CashShift),request.Id);
        if(shift.Status is not (CashShiftStatus.Open or CashShiftStatus.Closing))
            throw new ConflictException("cash_shift_not_open","الوردية ليست مفتوحة.");
        return await balance.CalculateExpectedAsync(shift,clock.GetUtcNow().UtcDateTime,ct);
    }
}
