using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseReturns;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed class ApplySupplierCreditCommandHandler(
    ISupplierCreditSettlementService credits,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ApplySupplierCreditCommand, SupplierCreditApplicationDto>
{
    public async Task<SupplierCreditApplicationDto> Handle(ApplySupplierCreditCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new ForbiddenException();

        var application = await credits.ApplyAsync(
            command.PurchaseReturnId,
            command.Request.PurchaseInvoiceId,
            command.Request.AppliedBaseAmount,
            command.Request.IdempotencyKey,
            timeProvider.GetUtcNow().UtcDateTime,
            currentUser.UserId,
            cancellationToken);

        return SupplierCreditMapper.ToDto(application);
    }
}
