using FluentValidation;

namespace OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;

public sealed class CreateJournalEntryCommandValidator
    : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Request.JournalType)
            .IsInEnum()
            .WithErrorCode("journal_type_invalid");

        RuleFor(x => x.Request.Description)
            .NotEmpty()
            .WithErrorCode("journal_description_required")
            .MaximumLength(500)
            .WithErrorCode("journal_description_max_length");

        RuleFor(x => x.Request.Lines)
            .NotNull()
            .WithErrorCode("journal_lines_required");

        RuleFor(x => x.Request.Lines)
            .Must(lines => lines is not null && lines.Count > 0)
            .WithErrorCode("journal_lines_required");

        RuleForEach(x => x.Request.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(x => x.AccountId)
                    .NotEmpty()
                    .WithErrorCode("journal_account_required");

                line.RuleFor(x => x.DebitAmount)
                    .GreaterThanOrEqualTo(0)
                    .WithErrorCode("journal_debit_invalid");

                line.RuleFor(x => x.CreditAmount)
                    .GreaterThanOrEqualTo(0)
                    .WithErrorCode("journal_credit_invalid");

                line.RuleFor(x => x.Description)
                    .MaximumLength(300)
                    .WithErrorCode("journal_line_description_max_length");

                line.RuleFor(x => x)
                    .Must(x => !(x.CustomerId.HasValue && x.SupplierId.HasValue))
                    .WithErrorCode("journal_line_party_conflict");
            });
    }
}