using MediatR;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.CustomerOrders.Specifications;
using OAS.Application.Sales.Services;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Application.Sales.CustomerOrders.Queries;

public sealed class GetCustomerOrdersQueryHandler(ICustomerOrderAggregateRepository repository, CustomerOrderSpecificationFactory specs, SalesDtoAssembler assembler)
    : IRequestHandler<GetCustomerOrdersQuery, PagedResult<CustomerOrderDto>>
{
    public async Task<PagedResult<CustomerOrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken ct)
    {
        var r = request.Request.Normalize();
        var page = await repository.GetPageAsync(specs.CreatePageSpecification(r), ct);
        var items = new List<CustomerOrderDto>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var full = await repository.GetAggregateAsync(item.Id, false, ct) ?? item;
            items.Add(await assembler.OrderAsync(full, ct));
        }
        return new PagedResult<CustomerOrderDto> { Items = items, PageNumber = r.PageNumber, PageSize = r.PageSize, TotalCount = page.TotalCount };
    }
}
