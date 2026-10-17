using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseReturns;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Queries;

public sealed record GetPurchaseReturnCreditSummaryQuery(Guid PurchaseReturnId)
    : IQuery<SupplierCreditSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.Receipts.View,
        AccountingPermissions.PaymentAllocations.View
    ];
}

public sealed class GetPurchaseReturnCreditSummaryQueryHandler(ISupplierCreditSettlementService credits)
    : IRequestHandler<GetPurchaseReturnCreditSummaryQuery, SupplierCreditSummaryDto>
{
    public async Task<SupplierCreditSummaryDto> Handle(
        GetPurchaseReturnCreditSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var snapshot = await credits.GetSnapshotAsync(query.PurchaseReturnId, cancellationToken);
        return new SupplierCreditSummaryDto(
            snapshot.PurchaseReturnId,
            snapshot.SupplierId,
            snapshot.TotalCreditBaseAmount,
            snapshot.AppliedCreditBaseAmount,
            snapshot.AvailableCreditBaseAmount,
            snapshot.Applications.Select(SupplierCreditMapper.ToDto).ToArray());
    }
}
