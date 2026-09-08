
using MediatR;
using OAS.Application.Abstractions.Numbering;

namespace OAS.Application.Features.Employees.Queries.GetNextEmployeeCode;

public sealed class GetNextEmployeeCodeQueryHandler(
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<GetNextEmployeeCodeQuery, string>
{
    public async Task<string> Handle(
        GetNextEmployeeCodeQuery request,
        CancellationToken cancellationToken)
    {
        var number = await sequenceNumberGenerator.NextAsync(
            "EmployeeNumberSequence",
            cancellationToken);

        return $"EMP-{number:D5}";
    }
}
