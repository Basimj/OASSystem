using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Journals.Mapping;
using OAS.Application.Accounting.Journals.Specifications;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Queries.GetJournalEntries;

public sealed class GetJournalEntriesQueryHandler(
    IReadRepository<JournalEntry, Guid> repository)
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

        var items =
            page.Items
                .Select(JournalEntryMapping.ToDto)
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