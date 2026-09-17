using FluentValidation;

namespace OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;

public sealed class SetJournalEntryStatusCommandValidator
    : AbstractValidator<SetJournalEntryStatusCommand>
{
    public SetJournalEntryStatusCommandValidator()
    {
        RuleFor(x => x.JournalEntryId)
            .NotEmpty()
            .WithErrorCode("journal_id_required");

        RuleFor(x => x.Request.Status)
            .IsInEnum()
            .WithErrorCode("journal_status_invalid");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required")
            .Must(IsValidBase64)
            .WithErrorCode("row_version_invalid");
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