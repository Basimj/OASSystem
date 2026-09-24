using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.SupplierAccounts.Mapping;
using OAS.Application.Accounting.SupplierAccounts.Specifications;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Queries.GetSupplierAccounts;

public sealed class GetSupplierAccountsQueryHandler(
    IReadRepository<SupplierAccount, Guid> repository,
    SupplierAccountMapper mapper)
    : IRequestHandler<GetSupplierAccountsQuery, PagedResult<SupplierAccountDto>>
{
    private static readonly SupplierAccountPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<SupplierAccountDto>> Handle(
        GetSupplierAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<SupplierAccountDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
