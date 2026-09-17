using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Specifications;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypes;

public sealed class GetExpenseTypesQueryHandler(
    IReadRepository<ExpenseType, Guid> repository,
    ExpenseTypeMapper mapper)
    : IRequestHandler<GetExpenseTypesQuery, PagedResult<ExpenseTypeDto>>
{
    private static readonly ExpenseTypePageSpecification SpecificationFactory = new();

    public async Task<PagedResult<ExpenseTypeDto>> Handle(
        GetExpenseTypesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<ExpenseTypeDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
