using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Services;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Returns.Queries;

/// <summary>
/// Returns posted invoices for exactly one customer, with paging applied in SQL.
/// The target credit command still enforces open balance, customer ownership and period policy.
/// </summary>
public sealed record GetCustomerCreditTargetInvoicesQuery(Guid CustomerId, int PageNumber = 1, int PageSize = 50)
    : IQuery<PagedResult<SalesInvoiceDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [SalesPermissions.Invoices.View, SalesPermissions.Credit.Use];
}

public sealed class GetCustomerCreditTargetInvoicesQueryHandler(
    ISalesInvoiceAggregateRepository invoices,
    SalesDtoAssembler assembler)
    : IRequestHandler<GetCustomerCreditTargetInvoicesQuery, PagedResult<SalesInvoiceDto>>
{
    public async Task<PagedResult<SalesInvoiceDto>> Handle(
        GetCustomerCreditTargetInvoicesQuery request, CancellationToken ct)
    {
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(request.CustomerId));

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var specification = new Specification<SalesInvoice>()
            .Where(x => x.CustomerId == request.CustomerId && x.Status == SalesInvoiceStatus.Posted)
            .AddSort(nameof(SalesInvoice.InvoiceDate), SortDirection.Descending)
            .ApplyPaging(checked((pageNumber - 1) * pageSize), pageSize);

        var result = await invoices.GetPageAsync(specification, ct);
        var items = new List<SalesInvoiceDto>(result.Items.Count);
        foreach (var invoice in result.Items)
        {
            var aggregate = await invoices.GetAggregateAsync(invoice.Id, false, ct) ?? invoice;
            items.Add(await assembler.InvoiceAsync(aggregate, ct));
        }

        return new PagedResult<SalesInvoiceDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }
}
