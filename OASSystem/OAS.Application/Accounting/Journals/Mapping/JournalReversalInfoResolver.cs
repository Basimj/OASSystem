using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Journals.Mapping;

public static class JournalReversalInfoResolver
{
    public static bool IsKnownReversal(JournalEntry journal) => journal.JournalType == JournalType.Reversal ||
        journal.SourceDocumentType is "JournalEntryReversal" or "CustomerSettlementFxReversal" or "SupplierSettlementFxReversal";

    public static async Task<IReadOnlyDictionary<Guid, JournalReversalInfoDto>> ResolveAsync(
        IReadRepository<JournalEntry, Guid> repository, IReadOnlyCollection<JournalEntry> journals, CancellationToken ct)
    {
        if (journals.Count == 0) return new Dictionary<Guid, JournalReversalInfoDto>();
        var ids = journals.Select(j => j.Id).ToArray();
        var linked = journals.Where(j => j.ReversedJournalId.HasValue).Select(j => j.ReversedJournalId!.Value).ToArray();
        var related = await repository.ListAsync(new Specification<JournalEntry>().Where(j => linked.Contains(j.Id) || (j.ReversedJournalId.HasValue && ids.Contains(j.ReversedJournalId.Value))), ct);
        return Build(journals, related);
    }

    public static IReadOnlyDictionary<Guid, JournalReversalInfoDto> Build(IEnumerable<JournalEntry> journals, IEnumerable<JournalEntry> related)
    {
        var current = journals.ToArray();
        var all = current.Concat(related).GroupBy(j => j.Id).ToDictionary(g => g.Key, g => g.First());
        var originals = all.Values.Where(j => j.ReversedJournalId.HasValue)
            .GroupBy(j => j.ReversedJournalId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(j => j.Id).First());
        return current.ToDictionary(j => j.Id, j =>
        {
            originals.TryGetValue(j.Id, out var original);
            JournalEntry? reversal = j.ReversedJournalId is Guid reversalId && all.TryGetValue(reversalId, out var found) ? found : null;
            bool isReversal = original is not null || IsKnownReversal(j);
            return new JournalReversalInfoDto(isReversal, original?.Id ?? (j.SourceDocumentType == "JournalEntryReversal" ? j.SourceDocumentId : null), original?.JournalNumber,
                j.ReversedJournalId, reversal?.JournalNumber, reversal is null ? null : (OAS.Contracts.Accounting.Enums.JournalEntryStatus)(byte)reversal.Status);
        });
    }

    public static JournalEntryDto Apply(JournalEntryDto dto, JournalReversalInfoDto info) => dto with
    {
        JournalType = info.IsReversal ? OAS.Contracts.Accounting.Enums.JournalType.Reversal : dto.JournalType,
        Reversal = info
    };
}
