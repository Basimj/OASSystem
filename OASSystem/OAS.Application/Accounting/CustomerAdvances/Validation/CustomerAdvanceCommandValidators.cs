using FluentValidation;
using OAS.Application.Accounting.CustomerAdvances.Commands;

namespace OAS.Application.Accounting.CustomerAdvances.Validation;

public sealed class CreateCustomerAdvanceCommandValidator : AbstractValidator<CreateCustomerAdvanceCommand>
{
    public CreateCustomerAdvanceCommandValidator()
    {
        RuleFor(x => x.Request.CustomerId).NotEmpty();
        RuleFor(x => x.Request.CustomerOrderId).NotEmpty();
        RuleFor(x => x.Request.ReceiptVoucherLineId).NotEmpty();
    }
}

public sealed class ApplyCustomerAdvanceCommandValidator : AbstractValidator<ApplyCustomerAdvanceCommand>
{
    public ApplyCustomerAdvanceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.SalesInvoiceId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0m);
        RuleFor(x => x.Request.RowVersion).NotEmpty();
    }
}
