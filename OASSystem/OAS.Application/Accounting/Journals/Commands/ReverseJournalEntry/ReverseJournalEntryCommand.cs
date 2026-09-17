using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;

namespace OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;

public sealed record ReverseJournalEntryCommand(
    Guid JournalEntryId)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.Reverse];
}