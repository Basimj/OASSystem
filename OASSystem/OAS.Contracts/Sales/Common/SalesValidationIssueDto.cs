namespace OAS.Contracts.Sales.Common;

public sealed record SalesValidationIssueDto(
    string Code,
    string Message,
    string? Field = null);
