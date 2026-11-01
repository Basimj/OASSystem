using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices.Specifications;

public sealed class SalesInvoiceSpecificationFactory : ICrudSpecificationFactory<SalesInvoice>
{
    public ISpecification<SalesInvoice> CreatePageSpecification(PageRequest request) =>
        CreatePageSpecification(request, null);

    public ISpecification<SalesInvoice> CreatePageSpecification(PageRequest request, Guid? customerId)
    {
        var r = request.Normalize();
        var spec = new Specification<SalesInvoice>();

        // Specification.Where replaces Criteria; compose both conditions into one
        // predicate so searches can never widen a customer-scoped result set.
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var q = r.Search.Trim();
            if (customerId.HasValue)
            {
                var id = customerId.Value;
                spec.Where(x => x.CustomerId == id &&
                    (x.InvoiceCode.Contains(q) || (x.Description != null && x.Description.Contains(q))));
            }
            else
            {
                spec.Where(x => x.InvoiceCode.Contains(q) ||
                    (x.Description != null && x.Description.Contains(q)));
            }
        }
        else if (customerId.HasValue)
        {
            var id = customerId.Value;
            spec.Where(x => x.CustomerId == id);
        }

        var sort = r.SortBy is nameof(SalesInvoice.InvoiceCode)
            or nameof(SalesInvoice.InvoiceDate)
            or nameof(SalesInvoice.PostingDate)
            or nameof(SalesInvoice.Status)
            or nameof(SalesInvoice.TotalAmount)
                ? r.SortBy! : nameof(SalesInvoice.InvoiceDate);
        spec.AddSort(sort, r.SortDirection)
            .ApplyPaging((r.PageNumber - 1) * r.PageSize, r.PageSize);
        return spec;
    }
}
