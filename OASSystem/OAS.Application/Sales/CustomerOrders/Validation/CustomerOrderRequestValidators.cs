using FluentValidation;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;

namespace OAS.Application.Sales.CustomerOrders.Validation;

public sealed class CreateCustomerOrderRequestValidator : AbstractValidator<CreateCustomerOrderRequest>
{
    public CreateCustomerOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.OrderCode).MaximumLength(40);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new CustomerOrderLineRequestValidator());
    }
}

public sealed class UpdateCustomerOrderRequestValidator : AbstractValidator<UpdateCustomerOrderRequest>
{
    public UpdateCustomerOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new CustomerOrderLineRequestValidator());
    }
}

public sealed class CustomerOrderLineRequestValidator : AbstractValidator<CustomerOrderLineRequest>
{
    public CustomerOrderLineRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.ActualUnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountValue).GreaterThanOrEqualTo(0).When(x => x.DiscountValue.HasValue);
        RuleFor(x => x.TaxRate).GreaterThanOrEqualTo(0).When(x => x.TaxRate.HasValue);
        RuleFor(x => x.ProductVariantId).NotEmpty().When(x => x.LineType is SalesLineType.Frame or SalesLineType.Lens or SalesLineType.Accessory);
        RuleFor(x => x.WarehouseId).NotEmpty().When(x => x.LineType is SalesLineType.Frame or SalesLineType.Lens or SalesLineType.Accessory);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
