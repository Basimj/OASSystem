namespace OAS.Contracts.Sales.Common;

public sealed record SalesConfirmationPreValidationDto(
    Guid InvoiceId,
    bool CanConfirm,
    IReadOnlyList<SalesValidationIssueDto> Issues,
    decimal? CreditLimit = null,
    decimal? ExposureBeforeCurrent = null,
    decimal? InvoiceBaseAmount = null,
    decimal? NewExposure = null);
