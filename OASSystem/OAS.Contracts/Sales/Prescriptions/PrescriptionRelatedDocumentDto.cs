namespace OAS.Contracts.Sales.Prescriptions;

/// <summary>Document with a stored header or line reference to a prescription revision.</summary>
public sealed record PrescriptionRelatedDocumentDto(
    Guid DocumentId,
    string DocumentCode,
    string DocumentType,
    DateOnly DocumentDate,
    string RevisionNumbers);
