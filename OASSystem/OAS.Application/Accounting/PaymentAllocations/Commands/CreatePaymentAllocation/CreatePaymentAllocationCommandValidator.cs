using FluentValidation;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;

public sealed class CreatePaymentAllocationCommandValidator
    : AbstractValidator<CreatePaymentAllocationCommand>
{
    public CreatePaymentAllocationCommandValidator()
    {
        RuleFor(x => x.Data.PaymentSourceId)
            .NotEmpty()
            .WithErrorCode("payment_source_id_required");

        RuleFor(x => x.Data.TargetDocumentId)
            .NotEmpty()
            .WithErrorCode("target_document_id_required");

        RuleFor(x => x.Data.AllocatedAmount)
            .GreaterThan(0)
            .WithErrorCode("allocated_amount_must_be_positive");
    }
}
