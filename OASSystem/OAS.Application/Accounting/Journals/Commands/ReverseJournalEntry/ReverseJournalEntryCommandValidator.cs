using FluentValidation;

namespace OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;

public sealed class ReverseJournalEntryCommandValidator
    : AbstractValidator<ReverseJournalEntryCommand>
{
    public ReverseJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId)
            .NotEmpty()
            .WithErrorCode("journal_id_required");
    }
}