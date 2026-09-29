using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Specifications;

public sealed class CustomerOrderSpecificationFactory : ICrudSpecificationFactory<CustomerOrder>
{
    public ISpecification<CustomerOrder> CreatePageSpecification(PageRequest request)
    {
        var r = request.Normalize();
        var spec = new Specification<CustomerOrder>();
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var q = r.Search;
            spec.Where(x => x.OrderCode.Contains(q!) || (x.Notes != null && x.Notes.Contains(q!)));
        }
        var sort = r.SortBy is nameof(CustomerOrder.OrderCode) or nameof(CustomerOrder.OrderDate) or nameof(CustomerOrder.Status) or nameof(CustomerOrder.TotalAmount)
            ? r.SortBy!
            : nameof(CustomerOrder.OrderDate);
        spec.AddSort(sort, r.SortDirection).ApplyPaging((r.PageNumber - 1) * r.PageSize, r.PageSize);
        return spec;
    }
}
