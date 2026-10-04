using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;
using DomainSalesReturnStatus = OAS.Domain.Sales.Enums.SalesReturnStatus;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class ConfirmSalesReturnCommandHandler(
    ISalesReturnAggregateRepository returns,
    ISalesReturnQueryService query,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ConfirmSalesReturnCommand, SalesReturnDto>
{
    public async Task<SalesReturnDto> Handle(ConfirmSalesReturnCommand request, CancellationToken ct)
    {
        var entity = await returns.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), request.Id);
        SalesConcurrency.Ensure(request.Request.RowVersion, entity.RowVersion, "مرتجع المبيعات");
        if (entity.Status != DomainSalesReturnStatus.Draft)
            throw new ConflictException("sales_return_invalid_status", "يمكن تأكيد مرتجع المبيعات من حالة المسودة فقط.");
        entity.Confirm(timeProvider.GetUtcNow(), currentUser.UserId);
        returns.Update(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return await query.GetByIdAsync(entity.Id, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), entity.Id);
    }
}
