using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.Mapping;
using OAS.Application.Accounting.Expenses.Specifications;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.Queries.GetExpenses;

public sealed class GetExpensesQueryHandler(
    IReadRepository<Expense, Guid> repository,
    ExpenseMapper mapper)
    : IRequestHandler<GetExpensesQuery, PagedResult<ExpenseDto>>
{
    private static readonly ExpensePageSpecification SpecificationFactory = new();

    public async Task<PagedResult<ExpenseDto>> Handle(
        GetExpensesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<ExpenseDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
