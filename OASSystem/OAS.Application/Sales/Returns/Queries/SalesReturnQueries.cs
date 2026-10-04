using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Returns;

namespace OAS.Application.Sales.Returns.Queries;

public sealed record GetSalesReturnByIdQuery(Guid Id) : IQuery<SalesReturnDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.View];
}

public sealed record GetSalesReturnsQuery(
    PageRequest Request,
    Guid? SalesInvoiceId = null,
    Guid? CustomerId = null,
    SalesReturnStatus? Status = null) : IQuery<PagedResult<SalesReturnDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.View];
}
