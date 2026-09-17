using FluentValidation;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;

public sealed class SetReceiptVoucherStatusCommandValidator
    : AbstractValidator<SetReceiptVoucherStatusCommand>
{
    public SetReceiptVoucherStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("receipt_voucher_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
