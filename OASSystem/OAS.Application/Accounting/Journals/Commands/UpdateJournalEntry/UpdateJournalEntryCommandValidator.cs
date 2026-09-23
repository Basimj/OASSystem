using FluentValidation;

namespace OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;

public sealed class UpdateJournalEntryCommandValidator
    : AbstractValidator<UpdateJournalEntryCommand>
{
    public UpdateJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId)
            .NotEmpty()
            .WithErrorCode("journal_id_required");

        RuleFor(x => x.Request.JournalType)
            .IsInEnum()
            .WithErrorCode("journal_type_invalid");

        RuleFor(x => x.Request.Description)
            .NotEmpty()
            .WithErrorCode("journal_description_required")
            .MaximumLength(500)
            .WithErrorCode("journal_description_max_length");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required")
            .Must(IsValidBase64)
            .WithErrorCode("row_version_invalid");

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

    private static bool IsValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}