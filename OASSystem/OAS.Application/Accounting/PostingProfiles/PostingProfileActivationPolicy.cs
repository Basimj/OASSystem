using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles;

/// <summary>
/// Enforces the invariant that only one active PostingProfile may exist
/// for the same Module + DocumentType pair.
///
/// Historical/inactive profiles are kept; activating a profile atomically
/// deactivates any other active profile for the same posting scope.
/// The database filtered unique index remains the final concurrency guard.
/// </summary>
internal static class PostingProfileActivationPolicy
{
    public static async Task DeactivateCompetingProfilesAsync(
        IRepository<PostingProfile, Guid> repository,
        Guid? currentProfileId,
        string module,
        string documentType,
        bool willBeActive,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!willBeActive)
            return;

        var normalizedModule = module?.Trim() ?? string.Empty;
        var normalizedDocumentType = documentType?.Trim() ?? string.Empty;

        if (normalizedModule.Length == 0 || normalizedDocumentType.Length == 0)
            return;

        // Keep this query deliberately broad and do the token comparison in-memory
        // with OrdinalIgnoreCase. This makes the behavior independent from SQL collation
        // and keeps the same semantics in application tests/fakes.
        var activeSpec = new Specification<PostingProfile>()
            .Where(x => x.IsActive)
            .Tracking();

        var activeProfiles = await repository.ListAsync(activeSpec, cancellationToken);

        foreach (var competingProfile in activeProfiles)
        {
            if (currentProfileId.HasValue && competingProfile.Id == currentProfileId.Value)
                continue;

            if (!string.Equals(competingProfile.Module, normalizedModule, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(competingProfile.DocumentType, normalizedDocumentType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            competingProfile.SetActive(false);
            repository.Update(competingProfile);
        }
    }
}
