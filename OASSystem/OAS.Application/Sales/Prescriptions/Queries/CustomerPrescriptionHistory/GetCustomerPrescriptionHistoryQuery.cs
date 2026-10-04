using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionHistory;

public sealed record GetCustomerPrescriptionHistoryQuery(Guid CustomerId)
    : IQuery<IReadOnlyList<PrescriptionHistoryItemDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.ViewCustomerPrescriptionContext];
}
