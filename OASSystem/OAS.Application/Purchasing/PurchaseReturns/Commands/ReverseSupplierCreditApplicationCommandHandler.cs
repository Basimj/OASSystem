using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseReturns;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed class ReverseSupplierCreditApplicationCommandHandler(
    ISupplierCreditSettlementService credits,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ReverseSupplierCreditApplicationCommand, SupplierCreditApplicationDto>
{
    public async Task<SupplierCreditApplicationDto> Handle(
        ReverseSupplierCreditApplicationCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new ForbiddenException();

        var application = await credits.ReverseAsync(
            command.PurchaseReturnId,
            command.ApplicationId,
            command.Request.RowVersion,
            command.Request.Reason,
            timeProvider.GetUtcNow().UtcDateTime,
            currentUser.UserId,
            cancellationToken);

        return SupplierCreditMapper.ToDto(application);
    }
}
