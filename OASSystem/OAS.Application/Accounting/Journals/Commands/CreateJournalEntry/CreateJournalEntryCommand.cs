using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Journals;

namespace OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;

public sealed record CreateJournalEntryCommand(
    CreateJournalEntryRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.Create];
}