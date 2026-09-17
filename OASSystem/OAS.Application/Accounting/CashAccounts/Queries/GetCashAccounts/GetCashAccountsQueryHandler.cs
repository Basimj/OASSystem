using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Accounting.CashAccounts.Specifications;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Queries.GetCashAccounts;

public sealed class GetCashAccountsQueryHandler(
    IReadRepository<CashAccount, Guid> repository,
    CashAccountMapper mapper)
    : IRequestHandler<GetCashAccountsQuery, PagedResult<CashAccountDto>>
{
    private static readonly CashAccountPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<CashAccountDto>> Handle(
        GetCashAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<CashAccountDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
