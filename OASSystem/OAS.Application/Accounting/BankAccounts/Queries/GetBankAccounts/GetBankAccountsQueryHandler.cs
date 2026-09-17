using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Accounting.BankAccounts.Specifications;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Queries.GetBankAccounts;

public sealed class GetBankAccountsQueryHandler(
    IReadRepository<BankAccount, Guid> repository,
    BankAccountMapper mapper)
    : IRequestHandler<GetBankAccountsQuery, PagedResult<BankAccountDto>>
{
    private static readonly BankAccountPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<BankAccountDto>> Handle(
        GetBankAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<BankAccountDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
