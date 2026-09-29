using FluentValidation;
using OAS.Application.Purchasing.Common;
using OAS.Application.Purchasing.PurchaseRequests.Commands;
using OAS.Contracts.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseRequests.Validators;

public sealed class CreatePurchaseRequestCommandValidator : AbstractValidator<CreatePurchaseRequestCommand>
{
    public CreatePurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Request.RequestType).IsInEnum();
        RuleFor(x => x.Request.WarehouseId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty().Must(x => x.Select(y => y.LineSequence).Distinct().Count() == x.Count).WithErrorCode("duplicate_line_sequence");
        RuleFor(x => x.Request).Must(x => !x.RequiredDate.HasValue || x.RequiredDate.Value >= x.RequestDate).WithErrorCode("required_date_before_request_date");
        RuleFor(x => x.Request).Must(x => x.RequestType != PurchaseRequestType.CustomerDemand || x.CustomerOrderId.HasValue).WithErrorCode("customer_order_required");
        RuleForEach(x => x.Request.Lines).ChildRules(line => { line.RuleFor(x => x.LineSequence).GreaterThan(0); line.RuleFor(x => x.ProductVariantId).NotEmpty(); line.RuleFor(x => x.RequestedQuantity).GreaterThan(0); line.RuleFor(x => x.Notes).MaximumLength(500); });
    }
}

public sealed class UpdatePurchaseRequestCommandValidator : AbstractValidator<UpdatePurchaseRequestCommand>
{
    public UpdatePurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");
        RuleFor(x => x.Request.RequestType).IsInEnum();
        RuleFor(x => x.Request.WarehouseId).NotEmpty();
        RuleFor(x => x.Request).Must(x => !x.RequiredDate.HasValue || x.RequiredDate.Value >= x.RequestDate).WithErrorCode("required_date_before_request_date");
        RuleFor(x => x.Request).Must(x => x.RequestType != PurchaseRequestType.CustomerDemand || x.CustomerOrderId.HasValue).WithErrorCode("customer_order_required");
        RuleFor(x => x.Request.Lines).NotEmpty().Must(x => x.Select(y => y.LineSequence).Distinct().Count() == x.Count).WithErrorCode("duplicate_line_sequence");
        RuleForEach(x => x.Request.Lines).ChildRules(line => { line.RuleFor(x => x.LineSequence).GreaterThan(0); line.RuleFor(x => x.ProductVariantId).NotEmpty(); line.RuleFor(x => x.RequestedQuantity).GreaterThan(0); line.RuleFor(x => x.RowVersion).Must(v => v is null || PurchasingRowVersion.IsValid(v)).WithErrorCode("row_version_invalid"); });
    }
}

public sealed class SubmitPurchaseRequestCommandValidator : AbstractValidator<SubmitPurchaseRequestCommand> { public SubmitPurchaseRequestCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");} }
public sealed class ApprovePurchaseRequestCommandValidator : AbstractValidator<ApprovePurchaseRequestCommand> { public ApprovePurchaseRequestCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");} }
public sealed class RejectPurchaseRequestCommandValidator : AbstractValidator<RejectPurchaseRequestCommand> { public RejectPurchaseRequestCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");RuleFor(x=>x.Request.Reason).NotEmpty().MaximumLength(500);} }
public sealed class CancelPurchaseRequestCommandValidator : AbstractValidator<CancelPurchaseRequestCommand> { public CancelPurchaseRequestCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");RuleFor(x=>x.Request.Reason).MaximumLength(500);} }

public sealed class CreatePurchaseOrderFromRequestCommandValidator : AbstractValidator<CreatePurchaseOrderFromRequestCommand>
{
    public CreatePurchaseOrderFromRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.SupplierId).NotEmpty();
        RuleFor(x => x.Request.DestinationWarehouseId).NotEmpty();
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.TaxCalculationMode).IsInEnum();
        RuleFor(x => x.Request.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Request.PaymentTermDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request).Must(x => !x.ExpectedDeliveryDate.HasValue || x.ExpectedDeliveryDate.Value >= x.OrderDate).WithErrorCode("expected_delivery_before_order_date");
        RuleFor(x => x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");
        RuleFor(x => x.Request.Allocations).NotEmpty().Must(x => x.Select(y => y.PurchaseRequestLineId).Distinct().Count() == x.Count).WithErrorCode("duplicate_request_line_allocation");
        RuleForEach(x => x.Request.Allocations).ChildRules(line => { line.RuleFor(x => x.PurchaseRequestLineId).NotEmpty(); line.RuleFor(x => x.AllocatedQuantity).GreaterThan(0); });
    }
}
