using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.SalesInvoices.Specifications;
using OAS.Application.Sales.Services;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Sales.SalesInvoices.Queries;

public sealed class GetSalesInvoicesQueryHandler(
    ISalesInvoiceAggregateRepository repository,
    IReadRepository<Customer, Guid> customers,
    SalesInvoiceSpecificationFactory specs,
    SalesDtoAssembler assembler)
    : IRequestHandler<GetSalesInvoicesQuery, PagedResult<SalesInvoiceDto>>
{
    public async Task<PagedResult<SalesInvoiceDto>> Handle(GetSalesInvoicesQuery request, CancellationToken ct)
    {
        var n = request.Request.Normalize();
        Guid[] matches = [];
        if (n.Search is { Length: > 0 } q)
        {
            // Query the customer directory before building the invoice EF predicate.
            // Customer filters remain scoped and are never replaced by this search.
            var found = await customers.ListAsync(new Specification<Customer>().Where(x =>
                x.NameAr.Contains(q) || x.CustomerCode.Contains(q) ||
                (x.NameEn != null && x.NameEn.Contains(q)) ||
                (x.ContactInfo.Mobile != null && x.ContactInfo.Mobile.Contains(q))), ct);
            matches = found.Select(x => x.Id).ToArray();
        }

        var page = await repository.GetPageAsync(
            specs.CreatePageSpecification(n, request.CustomerId, matches), ct);
        var items = new List<SalesInvoiceDto>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var full = await repository.GetAggregateAsync(item.Id, false, ct) ?? item;
            items.Add(await assembler.InvoiceAsync(full, ct));
        }
        return new PagedResult<SalesInvoiceDto>
        {
            Items = items, PageNumber = n.PageNumber, PageSize = n.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
