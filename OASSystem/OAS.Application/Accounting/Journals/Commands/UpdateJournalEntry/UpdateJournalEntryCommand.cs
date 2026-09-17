using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Journals;

namespace OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;

public sealed record UpdateJournalEntryCommand(
    Guid JournalEntryId,
    UpdateJournalEntryRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.Edit];
}