using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.Accounts.Specifications;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Queries.GetAccounts;

public sealed class GetAccountsQueryHandler(
    IReadRepository<Account, Guid> repository,
    AccountMapper mapper)
    : IRequestHandler<GetAccountsQuery, PagedResult<AccountDto>>
{
    private static readonly AccountPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<AccountDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<AccountDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
