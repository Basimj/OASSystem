using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees;

namespace OAS.Application.Features.Employees.Commands.ReserveEmployeeNumber;

public sealed class ReserveEmployeeNumberCommandHandler(ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<ReserveEmployeeNumberCommand, EmployeeNumberReservationDto>
{
    public async Task<EmployeeNumberReservationDto> Handle(ReserveEmployeeNumberCommand request, CancellationToken cancellationToken)
    {
        var next = await sequenceNumberGenerator.NextAsync("EmployeeNumberSequence", cancellationToken);
        if (next <= 0 || next > int.MaxValue)
            throw new ConflictException("employee_number_exhausted", "Employee number sequence exceeded the supported range.");

        var number = checked((int)next);
        return new EmployeeNumberReservationDto(number, EmployeeCodeFormatter.Format(number));
    }
}
