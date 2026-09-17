using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Queries.GetJournalEntryById;

public sealed record GetJournalEntryByIdQuery(
    Guid Id)
    : GetEntityByIdQuery<
        JournalEntry,
        Guid,
        JournalEntryDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.View];
}