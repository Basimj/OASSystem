using FluentValidation;
using OAS.Application.Purchasing.Common;
using OAS.Application.Purchasing.PurchaseOrders.Commands;

namespace OAS.Application.Purchasing.PurchaseOrders.Validators;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Request.SupplierId).NotEmpty();
        RuleFor(x => x.Request.DestinationWarehouseId).NotEmpty();
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.TaxCalculationMode).IsInEnum();
        RuleFor(x => x.Request.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Request.PaymentTermDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request).Must(x => !x.ExpectedDeliveryDate.HasValue || x.ExpectedDeliveryDate.Value >= x.OrderDate).WithErrorCode("expected_delivery_before_order_date");
        RuleFor(x => x.Request.Lines).NotEmpty().Must(x => x.Select(y=>y.LineSequence).Distinct().Count()==x.Count).WithErrorCode("duplicate_line_sequence");
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.LineSequence).GreaterThan(0); line.RuleFor(x=>x.ProductVariantId).NotEmpty(); line.RuleFor(x=>x.PurchaseUnitId).NotEmpty();
            line.RuleFor(x=>x.UnitConversionFactor).GreaterThan(0); line.RuleFor(x=>x.OrderedQuantity).GreaterThan(0); line.RuleFor(x=>x.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(x=>x.DiscountAmount).GreaterThanOrEqualTo(0); line.RuleFor(x=>x.TaxRate).GreaterThanOrEqualTo(0);
            line.RuleForEach(x=>x.Sources).ChildRules(source=>{source.RuleFor(x=>x.PurchaseRequestLineId).NotEmpty();source.RuleFor(x=>x.AllocatedQuantity).GreaterThan(0);});
        });
    }
}

public sealed class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x=>x.Id).NotEmpty(); RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");
        RuleFor(x=>x.Request.SupplierId).NotEmpty(); RuleFor(x=>x.Request.DestinationWarehouseId).NotEmpty(); RuleFor(x=>x.Request.CurrencyId).NotEmpty(); RuleFor(x=>x.Request.TaxCalculationMode).IsInEnum();
        RuleFor(x=>x.Request.ExchangeRate).GreaterThan(0); RuleFor(x=>x.Request.PaymentTermDays).GreaterThanOrEqualTo(0);
        RuleFor(x=>x.Request).Must(x=>!x.ExpectedDeliveryDate.HasValue || x.ExpectedDeliveryDate.Value >= x.OrderDate).WithErrorCode("expected_delivery_before_order_date");
        RuleFor(x=>x.Request.Lines).NotEmpty().Must(x=>x.Select(y=>y.LineSequence).Distinct().Count()==x.Count).WithErrorCode("duplicate_line_sequence");
        RuleForEach(x=>x.Request.Lines).ChildRules(line=>
        {
            line.RuleFor(x=>x.LineSequence).GreaterThan(0); line.RuleFor(x=>x.ProductVariantId).NotEmpty(); line.RuleFor(x=>x.PurchaseUnitId).NotEmpty();
            line.RuleFor(x=>x.UnitConversionFactor).GreaterThan(0); line.RuleFor(x=>x.OrderedQuantity).GreaterThan(0); line.RuleFor(x=>x.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(x=>x.DiscountAmount).GreaterThanOrEqualTo(0); line.RuleFor(x=>x.TaxRate).GreaterThanOrEqualTo(0);
            line.RuleFor(x=>x.RowVersion).Must(v=>v is null||PurchasingRowVersion.IsValid(v)).WithErrorCode("row_version_invalid");
            line.RuleForEach(x=>x.Sources).ChildRules(source=>{source.RuleFor(x=>x.PurchaseRequestLineId).NotEmpty();source.RuleFor(x=>x.AllocatedQuantity).GreaterThan(0);source.RuleFor(x=>x.RowVersion).Must(v=>v is null||PurchasingRowVersion.IsValid(v)).WithErrorCode("row_version_invalid");});
        });
    }
}

public sealed class SubmitPurchaseOrderCommandValidator : AbstractValidator<SubmitPurchaseOrderCommand> { public SubmitPurchaseOrderCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);} }
public sealed class ApprovePurchaseOrderCommandValidator : AbstractValidator<ApprovePurchaseOrderCommand> { public ApprovePurchaseOrderCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);} }
public sealed class RejectPurchaseOrderCommandValidator : AbstractValidator<RejectPurchaseOrderCommand> { public RejectPurchaseOrderCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.Reason).NotEmpty().MaximumLength(500);RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);} }
public sealed class SendPurchaseOrderCommandValidator : AbstractValidator<SendPurchaseOrderCommand> { public SendPurchaseOrderCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);} }
public sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand> { public CancelPurchaseOrderCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.Reason).MaximumLength(500);RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);} }
public sealed class ClosePurchaseOrderCommandValidator : AbstractValidator<ClosePurchaseOrderCommand> { public ClosePurchaseOrderCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);} }
