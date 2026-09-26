using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashAccounts.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.ReserveCashAccountCode;

public sealed class ReserveCashAccountCodeCommandHandler(
    ISequenceNumberGenerator sequences,
    IReadRepository<CashAccount, Guid> repository)
    : IRequestHandler<ReserveCashAccountCodeCommand, CashAccountCodeReservationDto>
{
    public async Task<CashAccountCodeReservationDto> Handle(
        ReserveCashAccountCodeCommand request,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; i++)
        {
            var code = CashAccountCodeFormatter.Format(
                await sequences.NextAsync("CashAccountCodeSequence", cancellationToken));

            if (await repository.CountAsync(
                    CashAccountSpecifications.ByCode(code),
                    cancellationToken) == 0)
            {
                return new CashAccountCodeReservationDto(code);
            }
        }

        throw new ConflictException(
            "accounting_cash_account_code_exhausted",
            "Unable to reserve a unique cash account code.");
    }
}
