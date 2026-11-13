using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.CustomerOrders.Specifications;

public sealed class CustomerOrderSpecificationFactory : ICrudSpecificationFactory<CustomerOrder>
{
    public ISpecification<CustomerOrder> CreatePageSpecification(PageRequest request)
    {
        var r = request.Normalize();
        var spec = new Specification<CustomerOrder>();
        // All filters are composed into one expression: Specification.Where replaces Criteria.
        var q = r.Search;
        var from = r.DateFrom;
        var to = r.DateTo;
        var hasStatus = Enum.TryParse<CustomerOrderStatus>(r.Status, true, out var parsedStatus)
            && Enum.IsDefined(parsedStatus);
        // Unknown status must never silently return unrestricted results.
        var statusInvalid = r.Status is not null && !hasStatus;
        spec.Where(x => !statusInvalid
            && (q == null || x.OrderCode.Contains(q)
                || (x.Notes != null && x.Notes.Contains(q))
                || (x.Customer != null && (x.Customer.CustomerCode.Contains(q)
                    || x.Customer.NameAr.Contains(q)
                    || (x.Customer.NameEn != null && x.Customer.NameEn.Contains(q))
                    || (x.Customer.ContactInfo.Mobile != null && x.Customer.ContactInfo.Mobile.Contains(q))
                    || (x.Customer.ContactInfo.Phone != null && x.Customer.ContactInfo.Phone.Contains(q)))))
            && (!hasStatus || x.Status == parsedStatus)
            && (!from.HasValue || x.OrderDate >= from.Value)
            && (!to.HasValue || x.OrderDate <= to.Value));
        var sort = r.SortBy is nameof(CustomerOrder.OrderCode) or nameof(CustomerOrder.OrderDate) or nameof(CustomerOrder.Status) or nameof(CustomerOrder.TotalAmount)
            ? r.SortBy!
            : nameof(CustomerOrder.OrderDate);
        spec.AddSort(sort, r.SortDirection)
            .AddSort(nameof(CustomerOrder.Id), r.SortDirection)
            .ApplyPaging((r.PageNumber - 1) * r.PageSize, r.PageSize);
        return spec;
    }
}
