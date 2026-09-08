namespace OAS.Contracts.Features.Employees;

public sealed record SetEmployeeStatusRequest(
    bool IsActive,
    string RowVersion);