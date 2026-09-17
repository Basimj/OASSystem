using FluentValidation;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.UpdateReceiptVoucher;

public sealed class UpdateReceiptVoucherCommandValidator
    : AbstractValidator<UpdateReceiptVoucherCommand>
{
    public UpdateReceiptVoucherCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("receipt_voucher_id_required");

        RuleFor(x => x.Data.TotalAmount)
            .GreaterThan(0)
            .WithErrorCode("total_amount_must_be_positive");

        RuleFor(x => x.Data.Lines)
            .NotEmpty()
            .WithErrorCode("receipt_voucher_lines_required");

        RuleForEach(x => x.Data.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId)
                .NotEmpty()
                .WithErrorCode("line_account_id_required");

            line.RuleFor(l => l.Amount)
                .GreaterThan(0)
                .WithErrorCode("line_amount_must_be_positive");
        });

        RuleFor(x => x.Data.ReceivedFrom)
            .MaximumLength(200)
            .WithErrorCode("received_from_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.ReceivedFrom));

        RuleFor(x => x.Data.Description)
            .MaximumLength(500)
            .WithErrorCode("description_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.Description));

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
