using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseReturns;
using DomainPurchaseReturnStatus = OAS.Domain.Purchasing.Enums.PurchaseReturnStatus;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed class ConfirmPurchaseReturnCommandHandler(
    IPurchaseReturnRepository returns,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<ConfirmPurchaseReturnCommand, PurchaseReturnDto>
{
    public async Task<PurchaseReturnDto> Handle(ConfirmPurchaseReturnCommand command, CancellationToken ct)
    {
        var entity = await returns.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException("PurchaseReturn", command.Id);
        PurchasingRowVersion.EnsureMatches(entity.RowVersion, command.Request.RowVersion, "Purchase return");
        if (entity.Status != DomainPurchaseReturnStatus.Draft)
            throw new ConflictException("purchase_return_invalid_status", "يمكن تأكيد مرتجع المشتريات من حالة المسودة فقط.");
        entity.Confirm(timeProvider.GetUtcNow(), currentUser.UserId);
        returns.Update(entity);
        return PurchaseReturnMapper.ToDto(entity);
    }
}
