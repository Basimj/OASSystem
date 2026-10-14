using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CoreHardening.Services;

public sealed class JournalSourceIntegrityService(
    IReadRepository<JournalEntry, Guid> journals) : IJournalSourceIntegrityService
{
    public async Task ValidateUniqueSourceAsync(
        string? sourceModule,
        string? sourceDocumentType,
        Guid? sourceDocumentId,
        Guid? excludingJournalId = null,
        CancellationToken cancellationToken = default)
    {
        var hasModule = !string.IsNullOrWhiteSpace(sourceModule);
        var hasType = !string.IsNullOrWhiteSpace(sourceDocumentType);
        var hasId = sourceDocumentId.HasValue && sourceDocumentId.Value != Guid.Empty;

        if (!hasModule && !hasType && !hasId)
            return;

        if (!hasModule || !hasType || !hasId)
        {
            throw new ConflictException(
                "accounting_journal_source_incomplete",
                "Source module, source document type and source document id must either all be provided or all be empty.");
        }

        var module = sourceModule!.Trim();
        var documentType = sourceDocumentType!.Trim();
        var documentId = sourceDocumentId!.Value;

        var spec = new Specification<JournalEntry>()
            .Where(x =>
                x.SourceModule == module &&
                x.SourceDocumentType == documentType &&
                x.SourceDocumentId == documentId &&
                (!excludingJournalId.HasValue || x.Id != excludingJournalId.Value))
            .ApplyPaging(0, 1);

        if (await journals.CountAsync(spec, cancellationToken) > 0)
        {
            throw new ConflictException(
                "accounting_source_already_posted",
                "An accounting journal already exists for this source document.");
        }
    }
}
