using FluentValidation;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.UpdatePaymentAllocation;

public sealed class UpdatePaymentAllocationCommandValidator
    : AbstractValidator<UpdatePaymentAllocationCommand>
{
    public UpdatePaymentAllocationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("payment_allocation_id_required");

        RuleFor(x => x.Data.AllocatedAmount)
            .GreaterThan(0)
            .WithErrorCode("allocated_amount_must_be_positive");
    }
}
