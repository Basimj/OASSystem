using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Returns;

namespace OAS.Application.Sales.Returns.Queries;

public sealed record GetCustomerCreditBalanceQuery(Guid CustomerId)
    : IQuery<CustomerCreditBalanceDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.View];
}
