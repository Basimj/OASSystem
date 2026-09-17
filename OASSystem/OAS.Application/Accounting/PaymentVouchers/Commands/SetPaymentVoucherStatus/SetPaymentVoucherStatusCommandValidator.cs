using FluentValidation;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;

public sealed class SetPaymentVoucherStatusCommandValidator
    : AbstractValidator<SetPaymentVoucherStatusCommand>
{
    public SetPaymentVoucherStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("payment_voucher_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
