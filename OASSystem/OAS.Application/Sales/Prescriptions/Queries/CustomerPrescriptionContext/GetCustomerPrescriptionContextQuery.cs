using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionContext;

public sealed record GetCustomerPrescriptionContextQuery(Guid CustomerId)
    : IQuery<CustomerPrescriptionContextDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.ViewCustomerPrescriptionContext];
}
