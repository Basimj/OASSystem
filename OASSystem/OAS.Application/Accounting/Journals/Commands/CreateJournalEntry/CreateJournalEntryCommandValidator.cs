using FluentValidation;
namespace OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;
public sealed class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Request.JournalType).IsInEnum().WithErrorCode("journal_type_invalid");
        RuleFor(x => x.Request.Description).NotEmpty().WithErrorCode("journal_description_required").MaximumLength(500).WithErrorCode("journal_description_max_length");
        RuleFor(x => x.Request.Lines).NotNull().Must(x => x is { Count: > 0 }).WithErrorCode("journal_lines_required");
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.AccountId).NotEmpty().WithErrorCode("journal_account_required");
            line.RuleFor(x => x.TransactionDebitAmount).GreaterThanOrEqualTo(0).WithErrorCode("journal_debit_invalid");
            line.RuleFor(x => x.TransactionCreditAmount).GreaterThanOrEqualTo(0).WithErrorCode("journal_credit_invalid");
            line.RuleFor(x => x.ExchangeRate).GreaterThan(0).When(x => x.ExchangeRate.HasValue).WithErrorCode("journal_exchange_rate_invalid");
            line.RuleFor(x => x.ExchangeRateType).IsInEnum().WithErrorCode("journal_exchange_rate_type_invalid");
            line.RuleFor(x => x.Description).MaximumLength(300).WithErrorCode("journal_line_description_max_length");
            line.RuleFor(x => x).Must(x => (x.TransactionDebitAmount > 0) != (x.TransactionCreditAmount > 0)).WithErrorCode("journal_line_amount_direction_invalid");
            line.RuleFor(x => x).Must(x => (x.CustomerId.HasValue ? 1 : 0) + (x.SupplierId.HasValue ? 1 : 0) + (x.EmployeeId.HasValue ? 1 : 0) <= 1).WithErrorCode("journal_line_party_conflict");
        });
    }
}
