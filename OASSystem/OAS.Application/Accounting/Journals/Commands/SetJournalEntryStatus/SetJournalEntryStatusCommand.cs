using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Journals;

namespace OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;

public sealed record SetJournalEntryStatusCommand(
    Guid JournalEntryId,
    SetJournalEntryStatusRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.Status];
}