using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.Journals.Queries.GetJournalEntries;

public sealed record GetJournalEntriesQuery(
    PageRequest Request)
    : IQuery<PagedResult<JournalEntryDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.View];
}