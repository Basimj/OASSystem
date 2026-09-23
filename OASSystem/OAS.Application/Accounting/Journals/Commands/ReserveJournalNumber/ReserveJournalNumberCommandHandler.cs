using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Common;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Commands.ReserveJournalNumber;

public sealed class ReserveJournalNumberCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<JournalEntry, Guid> repository)
    : IRequestHandler<ReserveJournalNumberCommand, AccountingNumberReservationDto>
{
    public async Task<AccountingNumberReservationDto> Handle(ReserveJournalNumberCommand request, CancellationToken cancellationToken)
    {
        var year = request.PostingDate.Year;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var sequence = await sequences.NextAsync($"JournalEntry-{year}", cancellationToken);
            var number = $"JV-{year:0000}-{sequence:000000}";
            var spec = new Specification<JournalEntry>().Where(x => x.JournalNumber == number);
            if (await repository.CountAsync(spec, cancellationToken) == 0)
                return new AccountingNumberReservationDto(number);
        }

        throw new InvalidOperationException("Unable to reserve a unique accounting number.");
    }
}
