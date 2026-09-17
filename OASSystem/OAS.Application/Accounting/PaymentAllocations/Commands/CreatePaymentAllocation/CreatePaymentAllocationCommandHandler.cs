using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Domain.Accounting.Entities;
using DomainAllocationTargetDocumentType = OAS.Domain.Accounting.Enums.AllocationTargetDocumentType;
using DomainPaymentSourceType = OAS.Domain.Accounting.Enums.PaymentSourceType;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;

public sealed class CreatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreatePaymentAllocationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreatePaymentAllocationCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var data = request.Data;

        var entity = PaymentAllocation.Create(
            Guid.NewGuid(),
            (DomainPaymentSourceType)(int)data.PaymentSourceType,
            data.PaymentSourceId,
            (DomainAllocationTargetDocumentType)(int)data.TargetDocumentType,
            data.TargetDocumentId,
            data.AllocatedAmount,
            timeProvider.GetUtcNow().UtcDateTime,
            userId);

        await repository.AddAsync(entity, cancellationToken);
        return entity.Id;
    }
}
