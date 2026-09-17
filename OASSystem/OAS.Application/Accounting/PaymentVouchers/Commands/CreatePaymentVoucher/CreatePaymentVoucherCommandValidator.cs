using FluentValidation;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;

public sealed class CreatePaymentVoucherCommandValidator
    : AbstractValidator<CreatePaymentVoucherCommand>
{
    public CreatePaymentVoucherCommandValidator()
    {
        RuleFor(x => x.Data.TotalAmount)
            .GreaterThan(0)
            .WithErrorCode("total_amount_must_be_positive");

        RuleFor(x => x.Data.Lines)
            .NotEmpty()
            .WithErrorCode("payment_voucher_lines_required");

        RuleForEach(x => x.Data.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId)
                .NotEmpty()
                .WithErrorCode("line_account_id_required");

            line.RuleFor(l => l.Amount)
                .GreaterThan(0)
                .WithErrorCode("line_amount_must_be_positive");
        });

        RuleFor(x => x.Data.BeneficiaryName)
            .MaximumLength(200)
            .WithErrorCode("beneficiary_name_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.BeneficiaryName));

        RuleFor(x => x.Data.Description)
            .MaximumLength(500)
            .WithErrorCode("description_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.Description));
    }
}
