using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices.Specifications;

public sealed class SalesInvoiceSpecificationFactory : ICrudSpecificationFactory<SalesInvoice>
{
    public ISpecification<SalesInvoice> CreatePageSpecification(PageRequest request)
    {
        var r=request.Normalize(); var spec=new Specification<SalesInvoice>();
        if(!string.IsNullOrWhiteSpace(r.Search)){var q=r.Search;spec.Where(x=>x.InvoiceCode.Contains(q!)||(x.Description!=null&&x.Description.Contains(q!)));}
        var sort=r.SortBy is nameof(SalesInvoice.InvoiceCode) or nameof(SalesInvoice.InvoiceDate) or nameof(SalesInvoice.PostingDate) or nameof(SalesInvoice.Status) or nameof(SalesInvoice.TotalAmount)?r.SortBy!:nameof(SalesInvoice.InvoiceDate);
        spec.AddSort(sort,r.SortDirection).ApplyPaging((r.PageNumber-1)*r.PageSize,r.PageSize);return spec;
    }
}
