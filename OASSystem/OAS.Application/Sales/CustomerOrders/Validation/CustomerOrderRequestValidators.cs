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
        RuleFor(x => x.PaymentPlan).IsInEnum();
        RuleFor(x => x.OrderDate)
            .NotEmpty()
            .WithMessage("يجب تحديد تاريخ الطلب.");
        RuleFor(x => x.RequiredDate)
            .Must((request, requiredDate) => !requiredDate.HasValue || requiredDate.Value >= request.OrderDate)
            .WithMessage("تاريخ التسليم المطلوب لا يمكن أن يكون قبل تاريخ الطلب.");
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
        RuleFor(x => x.PaymentPlan).IsInEnum();
        RuleFor(x => x.OrderDate)
            .NotEmpty()
            .WithMessage("يجب تحديد تاريخ الطلب.");
        RuleFor(x => x.RequiredDate)
            .Must((request, requiredDate) => !requiredDate.HasValue || requiredDate.Value >= request.OrderDate)
            .WithMessage("تاريخ التسليم المطلوب لا يمكن أن يكون قبل تاريخ الطلب.");
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
        // Warehouse depends on the selected product's IsStockItem flag and is therefore
        // validated authoritatively by ISalesLineResolver. Non-stock optical products must
        // remain valid without a warehouse.
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
        When(x => x.OpticalSnapshot is not null, () =>
        {
            RuleFor(x => x.OpticalSnapshot!.MeasurementSource).IsInEnum();
            RuleFor(x => x.OpticalSnapshot!.Eye).IsInEnum();
            RuleFor(x => x.OpticalSnapshot!.PrescriptionRevisionId)
                .NotEmpty()
                .When(x => x.OpticalSnapshot!.MeasurementSource == OpticalMeasurementSource.StoredPrescription);
            RuleFor(x => x.OpticalSnapshot!.Axis)
                .InclusiveBetween((short)0, (short)180)
                .When(x => x.OpticalSnapshot!.Axis.HasValue);
            RuleFor(x => x.OpticalSnapshot!.ADD)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.OpticalSnapshot!.ADD.HasValue);
            RuleFor(x => x.OpticalSnapshot!.Prism)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.OpticalSnapshot!.Prism.HasValue);
            RuleFor(x => x.OpticalSnapshot!.PD)
                .GreaterThan(0m)
                .When(x => x.OpticalSnapshot!.PD.HasValue);
            RuleFor(x => x.OpticalSnapshot!.MonocularPD)
                .GreaterThan(0m)
                .When(x => x.OpticalSnapshot!.MonocularPD.HasValue);
            RuleFor(x => x.OpticalSnapshot!.FittingHeight)
                .GreaterThan(0m)
                .When(x => x.OpticalSnapshot!.FittingHeight.HasValue);
            RuleFor(x => x.OpticalSnapshot!.VA).MaximumLength(20);
            RuleFor(x => x.OpticalSnapshot!.LensTypeSnapshot).MaximumLength(100);
            RuleFor(x => x.OpticalSnapshot!.MaterialSnapshot).MaximumLength(100);
            RuleFor(x => x.OpticalSnapshot!.CoatingSnapshot).MaximumLength(100);
        });
    }
}
