using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainCashShiftStatus = OAS.Domain.Accounting.Enums.CashShiftStatus;

namespace OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;

public sealed class CreateCashShiftCommandHandler(
    IRepository<CashShift, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreateCashShiftCommand, Guid>
{
    public async Task<Guid> Handle(CreateCashShiftCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) throw new ForbiddenException();
        var now=timeProvider.GetUtcNow().UtcDateTime;
        var number=await ResolveNumberAsync(request.Data.ShiftNumber,now.Year,cancellationToken);
        var shift=CashShift.Create(Guid.NewGuid(),number,request.Data.CashAccountId,userId,now,request.Data.OpeningBalance,DomainCashShiftStatus.Open);
        await repository.AddAsync(shift,cancellationToken);
        return shift.Id;
    }
    private async Task<string> ResolveNumberAsync(string? requested,int year,CancellationToken ct)
    {
        var number=requested?.Trim();
        if(!string.IsNullOrEmpty(number)&&!number.StartsWith($"CS-{year:0000}-",StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("cash_shift_number_period_mismatch","Reserved cash shift number does not match the current year.");
        if(string.IsNullOrEmpty(number))
        {
            for(var attempt=0;attempt<100;attempt++)
            {
                var sequence=await sequenceNumberGenerator.NextAsync($"CashShift-{year}",ct);
                number=$"CS-{year:0000}-{sequence:000000}";
                if(!await ExistsAsync(number,ct)) break;
            }
        }
        if(string.IsNullOrEmpty(number)||await ExistsAsync(number,ct)) throw new ConflictException("cash_shift_number_duplicate","Cash shift number is already in use.");
        return number;
    }
    private async Task<bool> ExistsAsync(string number,CancellationToken ct)=>await repository.CountAsync(new Specification<CashShift>().Where(x=>x.ShiftNumber==number),ct)>0;
}
