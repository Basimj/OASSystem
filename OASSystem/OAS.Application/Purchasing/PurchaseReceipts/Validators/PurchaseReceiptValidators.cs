using FluentValidation;
using OAS.Application.Purchasing.Common;
using OAS.Application.Purchasing.PurchaseReceipts.Commands;

namespace OAS.Application.Purchasing.PurchaseReceipts.Validators;

public sealed class CreatePurchaseReceiptCommandValidator:AbstractValidator<CreatePurchaseReceiptCommand>
{
    public CreatePurchaseReceiptCommandValidator(){RuleFor(x=>x.Request.PurchaseOrderId).NotEmpty();RuleFor(x=>x.Request.Lines).NotEmpty().Must(x=>x.Select(y=>y.PurchaseOrderLineId).Distinct().Count()==x.Count).WithErrorCode("duplicate_po_line");RuleForEach(x=>x.Request.Lines).ChildRules(l=>{l.RuleFor(x=>x.PurchaseOrderLineId).NotEmpty();l.RuleFor(x=>x.LineSequence).GreaterThan(0);l.RuleFor(x=>x.ReceivedQuantity).GreaterThan(0);l.RuleFor(x=>x.AcceptedQuantity).GreaterThanOrEqualTo(0);l.RuleFor(x=>x.RejectedQuantity).GreaterThanOrEqualTo(0);l.RuleFor(x=>x.ActualUnitCost).GreaterThanOrEqualTo(0);l.RuleFor(x=>x).Must(x=>x.AcceptedQuantity+x.RejectedQuantity==x.ReceivedQuantity).WithErrorCode("receipt_quantity_split_invalid");});}
}
public sealed class UpdatePurchaseReceiptCommandValidator:AbstractValidator<UpdatePurchaseReceiptCommand>
{
    public UpdatePurchaseReceiptCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");RuleFor(x=>x.Request.Lines).NotEmpty().Must(x=>x.Select(y=>y.PurchaseOrderLineId).Distinct().Count()==x.Count).WithErrorCode("duplicate_po_line");RuleForEach(x=>x.Request.Lines).ChildRules(l=>{l.RuleFor(x=>x.PurchaseOrderLineId).NotEmpty();l.RuleFor(x=>x.LineSequence).GreaterThan(0);l.RuleFor(x=>x.ReceivedQuantity).GreaterThan(0);l.RuleFor(x=>x.AcceptedQuantity).GreaterThanOrEqualTo(0);l.RuleFor(x=>x.RejectedQuantity).GreaterThanOrEqualTo(0);l.RuleFor(x=>x.ActualUnitCost).GreaterThanOrEqualTo(0);l.RuleFor(x=>x.RowVersion).Must(v=>v is null||PurchasingRowVersion.IsValid(v)).WithErrorCode("row_version_invalid");l.RuleFor(x=>x).Must(x=>x.AcceptedQuantity+x.RejectedQuantity==x.ReceivedQuantity).WithErrorCode("receipt_quantity_split_invalid");});}
}
public sealed class ConfirmPurchaseReceiptCommandValidator:AbstractValidator<ConfirmPurchaseReceiptCommand>{public ConfirmPurchaseReceiptCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);}}
public sealed class PostPurchaseReceiptCommandValidator:AbstractValidator<PostPurchaseReceiptCommand>{public PostPurchaseReceiptCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);}}
public sealed class CancelPurchaseReceiptCommandValidator:AbstractValidator<CancelPurchaseReceiptCommand>{public CancelPurchaseReceiptCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.Reason).MaximumLength(500);RuleFor(x=>x.Request.RowVersion).Must(PurchasingRowVersion.IsValid);}}
