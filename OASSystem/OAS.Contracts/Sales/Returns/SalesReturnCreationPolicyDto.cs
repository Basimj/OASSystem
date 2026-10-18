namespace OAS.Contracts.Sales.Returns;

/// <summary>Effective return dates from historical invoice posting or optical delivery.</summary>
public sealed record SalesReturnCreationPolicyDto(
    Guid SalesInvoiceId, bool CanCreate, DateOnly? EarliestReturnDate,
    DateOnly? LastReturnDateWithoutOverride, string? Reason, bool CanOverrideReturnWindow = false);
