using FluentValidation;
namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;
public sealed class CreatePaymentAllocationCommandValidator : AbstractValidator<CreatePaymentAllocationCommand>
{
    public CreatePaymentAllocationCommandValidator()
    {
        RuleFor(x => x.Data).Must(x => (x.ReceiptVoucherLineId.HasValue ? 1 : 0) + (x.PaymentVoucherLineId.HasValue ? 1 : 0) == 1)
            .WithErrorCode("payment_source_line_required");
        RuleFor(x => x.Data.TargetDocumentType).IsInEnum().WithErrorCode("target_document_type_invalid");
        RuleFor(x => x.Data.TargetDocumentId).NotEmpty().WithErrorCode("target_document_id_required");
        RuleFor(x => x.Data.AllocatedAmount).GreaterThan(0).WithErrorCode("allocated_amount_must_be_positive");
    }
}
