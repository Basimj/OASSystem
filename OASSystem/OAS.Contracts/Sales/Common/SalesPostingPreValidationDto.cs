namespace OAS.Contracts.Sales.Common;

public sealed record SalesPostingPreValidationDto(
    Guid InvoiceId,
    bool CanPost,
    IReadOnlyList<SalesValidationIssueDto> Issues);
