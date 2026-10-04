using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Domain.Exceptions;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed class CancelPurchaseReturnCommandHandler(
    IPurchaseReturnRepository returns,
    IPurchaseReceiptRepository receipts,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CancelPurchaseReturnCommand, PurchaseReturnDto>
{
    public async Task<PurchaseReturnDto> Handle(CancelPurchaseReturnCommand command, CancellationToken ct)
    {
        var entity = await returns.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException("PurchaseReturn", command.Id);
        PurchasingRowVersion.EnsureMatches(entity.RowVersion, command.Request.RowVersion, "Purchase return");
        var receipt = await receipts.GetForUpdateAsync(entity.PurchaseReceiptId, ct) ?? throw new NotFoundException("PurchaseReceipt", entity.PurchaseReceiptId);
        foreach (var line in entity.Lines.Where(x => x.IsActive))
        {
            var source = receipt.Lines.SingleOrDefault(x => x.Id == line.PurchaseReceiptLineId)
                ?? throw new ConflictException("purchase_return_receipt_line_missing", "تعذر العثور على سطر الاستلام الأصلي أثناء إلغاء المرتجع.");
            try { source.ReleaseReturnQuantity(line.Quantity); }
            catch (DomainException ex) { throw new ConflictException("purchase_return_release_invalid", ex.Message); }
        }
        receipts.Update(receipt);
        entity.Cancel(timeProvider.GetUtcNow(), currentUser.UserId, command.Request.Reason);
        returns.Update(entity);
        return PurchaseReturnMapper.ToDto(entity);
    }
}
