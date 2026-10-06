using FluentValidation;
using OAS.Application.Sales.Checkout.Commands;
using OAS.Contracts.Sales.Enums;

namespace OAS.Application.Sales.Checkout.Validation;

public sealed class CheckoutCustomerOrderValidator : AbstractValidator<CheckoutCustomerOrderCommand>
{
    public CheckoutCustomerOrderValidator()
    {
        RuleFor(x => x.Request.CustomerOrderId).NotEmpty();
        RuleFor(x => x.Request.PaymentPlan).IsInEnum();
        RuleFor(x => x.Request.RowVersion).NotEmpty();
        RuleFor(x => x.Request.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleForEach(x => x.Request.PaymentLines).ChildRules(line =>
        {
            line.RuleFor(x => x.CurrencyId).NotEmpty();
            line.RuleFor(x => x.Amount).GreaterThan(0m);
            line.RuleFor(x => x.PaymentMethod).IsInEnum();
        });
        RuleForEach(x => x.Request.SupplierSchedulingDecisions).ChildRules(line =>
        {
            line.RuleFor(x => x.CustomerOrderLineId).NotEmpty();
        });
    }
}

public sealed class DeliverCustomerOrderValidator : AbstractValidator<DeliverCustomerOrderCommand>
{
    public DeliverCustomerOrderValidator()
    {
        RuleFor(x => x.CustomerOrderId).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty();
        RuleFor(x => x.Request.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleForEach(x => x.Request.PaymentLines).ChildRules(line =>
        {
            line.RuleFor(x => x.CurrencyId).NotEmpty();
            line.RuleFor(x => x.Amount).GreaterThan(0m);
            line.RuleFor(x => x.PaymentMethod).IsInEnum();
        });
    }
}
