using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices.Specifications;

public sealed class SalesInvoiceSpecificationFactory : ICrudSpecificationFactory<SalesInvoice>
{
    public ISpecification<SalesInvoice> CreatePageSpecification(PageRequest request) =>
        CreatePageSpecification(request, null, []);

    public ISpecification<SalesInvoice> CreatePageSpecification(PageRequest request, Guid? customerId) =>
        CreatePageSpecification(request, customerId, []);

    public ISpecification<SalesInvoice> CreatePageSpecification(
        PageRequest request, Guid? customerId, Guid[] matchingCustomerIds)
    {
        var r = request.Normalize();
        var spec = new Specification<SalesInvoice>();
        var search = r.Search;
        var from = r.DateFrom;
        var to = r.DateTo;
        var hasStatus = !string.IsNullOrWhiteSpace(r.Status);
        var statusValid = !hasStatus || Enum.TryParse<OAS.Domain.Sales.Enums.SalesInvoiceStatus>(
            r.Status, true, out _);
        var selectedStatus = hasStatus && statusValid
            ? Enum.Parse<OAS.Domain.Sales.Enums.SalesInvoiceStatus>(r.Status!, true)
            : default;

        // Conditions are combined; repeated Specification.Where calls replace earlier criteria.
        // Domain and Contracts statuses are different enums, so convert before EF translation.
        spec.Where(x =>
            (!customerId.HasValue || x.CustomerId == customerId.Value) &&
            (!hasStatus || (statusValid && x.Status == selectedStatus)) &&
            (!from.HasValue || x.InvoiceDate >= from.Value) &&
            (!to.HasValue || x.InvoiceDate <= to.Value) &&
            (search == null || x.InvoiceCode.Contains(search) ||
             (x.Description != null && x.Description.Contains(search)) ||
             matchingCustomerIds.Contains(x.CustomerId)));

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
