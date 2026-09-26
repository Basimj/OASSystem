using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.BankAccounts.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.ReserveBankAccountCode;

public sealed class ReserveBankAccountCodeCommandHandler(
    ISequenceNumberGenerator sequences,
    IReadRepository<BankAccount, Guid> repository)
    : IRequestHandler<ReserveBankAccountCodeCommand, BankAccountCodeReservationDto>
{
    public async Task<BankAccountCodeReservationDto> Handle(
        ReserveBankAccountCodeCommand request,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; i++)
        {
            var code = BankAccountCodeFormatter.Format(
                await sequences.NextAsync("BankAccountCodeSequence", cancellationToken));

            if (await repository.CountAsync(
                    BankAccountSpecifications.ByCode(code),
                    cancellationToken) == 0)
            {
                return new BankAccountCodeReservationDto(code);
            }
        }

        throw new ConflictException(
            "accounting_bank_account_code_exhausted",
            "Unable to reserve a unique bank account code.");
    }
}
