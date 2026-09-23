using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Common;

namespace OAS.Application.Accounting.Journals.Commands.ReserveJournalNumber;

public sealed record ReserveJournalNumberCommand(DateOnly PostingDate) : ICommand<AccountingNumberReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.Create];
}
