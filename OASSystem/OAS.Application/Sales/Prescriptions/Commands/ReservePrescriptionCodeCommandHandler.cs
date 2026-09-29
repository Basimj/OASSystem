using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed class ReservePrescriptionCodeCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<Prescription, Guid> repository)
    : IRequestHandler<ReservePrescriptionCodeCommand, SalesCodeReservationDto>
{
    public async Task<SalesCodeReservationDto> Handle(ReservePrescriptionCodeCommand request, CancellationToken ct)
    {
        for (var i = 0; i < 100; i++)
        {
            var code = PrescriptionCodeFormatter.Format(await sequences.NextAsync("PrescriptionCodeSequence", ct));
            if (await repository.CountAsync(new Specification<Prescription>().Where(x => x.PrescriptionCode == code), ct) == 0)
                return new SalesCodeReservationDto(code);
        }
        throw new InvalidOperationException("Unable to reserve a unique prescription code.");
    }
}
