using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CustomerAccounts.Mapping;
using OAS.Application.Accounting.CustomerAccounts.Specifications;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Queries.GetCustomerAccounts;

public sealed class GetCustomerAccountsQueryHandler(
    IReadRepository<CustomerAccount, Guid> repository,
    CustomerAccountMapper mapper)
    : IRequestHandler<GetCustomerAccountsQuery, PagedResult<CustomerAccountDto>>
{
    private static readonly CustomerAccountPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<CustomerAccountDto>> Handle(
        GetCustomerAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<CustomerAccountDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
