using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Journals.Mapping;
using OAS.Application.Accounting.Journals.Specifications;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Queries.GetJournalEntries;

public sealed class GetJournalEntriesQueryHandler(
    IReadRepository<JournalEntry, Guid> repository,
    IReadRepository<JournalEntryLine, Guid> lineRepository)
    : IRequestHandler<
        GetJournalEntriesQuery,
        PagedResult<JournalEntryDto>>
{
    public async Task<PagedResult<JournalEntryDto>> Handle(
        GetJournalEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized =
            request.Request.Normalize();

        var specification =
            new JournalEntryPageSpecification(
                normalized);

        var page =
            await repository.GetPageAsync(
                specification,
                cancellationToken);

        // Fetch only the lines belonging to the selected page in one additional query.
        // Journal totals are base-currency amounts and must not be guessed from the header.
        var pageIds = page.Items.Select(x => x.Id).ToArray();
        var linesByJournal = new Dictionary<Guid, JournalEntryLine[]>();
        if (pageIds.Length > 0)
        {
            var lineSpec = new Specification<JournalEntryLine>()
                .Where(line => pageIds.Contains(line.JournalEntryId));
            var lines = await lineRepository.ListAsync(lineSpec, cancellationToken);
            linesByJournal = lines.GroupBy(x => x.JournalEntryId)
                .ToDictionary(x => x.Key, x => x.ToArray());
        }

        var items = page.Items
            .Select(journal => JournalEntryMapping.ToDto(journal,
                linesByJournal.TryGetValue(journal.Id, out var journalLines)
                    ? journalLines
                    : Array.Empty<JournalEntryLine>()))
            .ToArray();

        return new PagedResult<JournalEntryDto>
        {
            Items = items,
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}