using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;
using DomainSalesReturnStatus = OAS.Domain.Sales.Enums.SalesReturnStatus;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class PostSalesReturnCommandHandler(
    ISalesReturnAggregateRepository returns,
    ISalesReturnInventoryPostingService inventory,
    ISalesReturnAccountingPostingService accounting,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PostSalesReturnCommand, SalesReturnPostingResultDto>
{
    public async Task<SalesReturnPostingResultDto> Handle(PostSalesReturnCommand request, CancellationToken ct)
    {
        var entity = await returns.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), request.Id);
        SalesConcurrency.Ensure(request.Request.RowVersion, entity.RowVersion, "مرتجع المبيعات");
        if (entity.Status == DomainSalesReturnStatus.Posted)
            throw new ConflictException("sales_return_already_posted", "مرتجع المبيعات مرحل مسبقًا.");
        if (entity.Status != DomainSalesReturnStatus.Confirmed)
            throw new ConflictException("sales_return_invalid_status", "يجب تأكيد مرتجع المبيعات قبل الترحيل.");
        if (!Guid.TryParse(currentUser.UserId, out var userId) || userId == Guid.Empty)
            throw new ForbiddenException();

        var now = timeProvider.GetUtcNow();
        var inventoryResult = await inventory.PostAsync(entity, currentUser.UserId!, now, ct);
        var journalId = await accounting.PostAsync(entity, userId, now.UtcDateTime, ct);
        entity.MarkPosted(journalId, now, currentUser.UserId);
        returns.Update(entity);
        return new SalesReturnPostingResultDto(entity.Id, journalId, inventoryResult.InventoryTransactionIds);
    }
}
