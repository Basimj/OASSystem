namespace OAS.Contracts.Accounting.Common;

public sealed record AccountingLookupDto(
    Guid Id,
    string Code,
    string Name);