using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseInvoiceReceiptAllocation : AuditableEntity<Guid>
{
    private PurchaseInvoiceReceiptAllocation() { }
    private PurchaseInvoiceReceiptAllocation(Guid id,Guid purchaseInvoiceLineId,Guid purchaseReceiptLineId,decimal matchedQuantity,decimal matchedNetAmount,decimal quantityVariance,decimal priceVarianceAmount,decimal taxVarianceAmount,PurchaseMatchStatus matchStatus)
    {Id=PurchasingDomainGuard.Required(id,"Purchase invoice receipt allocation id");PurchaseInvoiceLineId=PurchasingDomainGuard.Required(purchaseInvoiceLineId,"Purchase invoice line id");PurchaseReceiptLineId=PurchasingDomainGuard.Required(purchaseReceiptLineId,"Purchase receipt line id");SetMatch(matchedQuantity,matchedNetAmount,quantityVariance,priceVarianceAmount,taxVarianceAmount,matchStatus);}
    public Guid PurchaseInvoiceLineId{get;private set;} public Guid PurchaseReceiptLineId{get;private set;} public decimal MatchedQuantity{get;private set;} public decimal MatchedNetAmount{get;private set;}
    public decimal QuantityVariance{get;private set;} public decimal PriceVarianceAmount{get;private set;} public decimal TaxVarianceAmount{get;private set;} public PurchaseMatchStatus MatchStatus{get;private set;}
    public string? ApprovalReason{get;private set;} public string? ApprovedBy{get;private set;} public DateTimeOffset? ApprovedAt{get;private set;} public byte[] RowVersion{get;private set;}=[];
    public static PurchaseInvoiceReceiptAllocation Create(Guid id,Guid purchaseInvoiceLineId,Guid purchaseReceiptLineId,decimal matchedQuantity,decimal matchedNetAmount,decimal quantityVariance,decimal priceVarianceAmount,decimal taxVarianceAmount,PurchaseMatchStatus matchStatus)=>new(id,purchaseInvoiceLineId,purchaseReceiptLineId,matchedQuantity,matchedNetAmount,quantityVariance,priceVarianceAmount,taxVarianceAmount,matchStatus);
    public void SetMatch(decimal matchedQuantity,decimal matchedNetAmount,decimal quantityVariance,decimal priceVarianceAmount,decimal taxVarianceAmount,PurchaseMatchStatus matchStatus){PurchasingDomainGuard.Positive(matchedQuantity,"Matched quantity");PurchasingDomainGuard.NonNegative(matchedNetAmount,"Matched net amount");PurchasingDomainGuard.Defined(matchStatus,"Match status");MatchedQuantity=matchedQuantity;MatchedNetAmount=matchedNetAmount;QuantityVariance=quantityVariance;PriceVarianceAmount=priceVarianceAmount;TaxVarianceAmount=taxVarianceAmount;MatchStatus=matchStatus;ApprovalReason=null;ApprovedBy=null;ApprovedAt=null;}
    public void ApproveVariance(DateTimeOffset at,string? by,string reason){if(MatchStatus!=PurchaseMatchStatus.RequiresApproval)throw new DomainException("Only variances requiring approval can be approved.");ApprovalReason=PurchasingDomainGuard.Required(reason,500,"Approval reason");ApprovedAt=at;ApprovedBy=PurchasingDomainGuard.User(by);MatchStatus=PurchaseMatchStatus.ApprovedVariance;}
    public void RejectVariance(string reason){if(MatchStatus!=PurchaseMatchStatus.RequiresApproval)throw new DomainException("Only variances requiring approval can be rejected.");ApprovalReason=PurchasingDomainGuard.Required(reason,500,"Rejection reason");MatchStatus=PurchaseMatchStatus.Rejected;}
}
