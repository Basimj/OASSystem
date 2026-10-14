namespace OAS.Application.Accounting.CoreHardening.Services;

public interface IJournalSourceIntegrityService
{
    Task ValidateUniqueSourceAsync(
        string? sourceModule,
        string? sourceDocumentType,
        Guid? sourceDocumentId,
        Guid? excludingJournalId = null,
        CancellationToken cancellationToken = default);
}
