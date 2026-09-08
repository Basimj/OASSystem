using MediatR;

namespace OAS.Application.Features.Employees.Queries.GetNextEmployeeCode;

public sealed record GetNextEmployeeCodeQuery
    : IRequest<string>;

