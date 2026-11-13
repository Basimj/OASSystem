using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Prescriptions.Specifications;
using OAS.Application.Sales.Services;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Sales.Prescriptions.Queries;

public sealed class GetPrescriptionsQueryHandler(
    IPrescriptionAggregateRepository repository,
    IReadRepository<Customer, Guid> customers,
    PrescriptionSpecificationFactory specs,
    SalesDtoAssembler assembler)
    : IRequestHandler<GetPrescriptionsQuery, PagedResult<PrescriptionDto>>
{
    public async Task<PagedResult<PrescriptionDto>> Handle(GetPrescriptionsQuery request, CancellationToken ct)
    {
        var normalized = request.Request.Normalize();
        Guid[] matchingCustomerIds = [];
        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var q = normalized.Search;
            var matchingCustomers = await customers.ListAsync(new Specification<Customer>().Where(x =>
                x.NameAr.Contains(q) || x.CustomerCode.Contains(q) ||
                (x.NameEn != null && x.NameEn.Contains(q)) ||
                (x.TradeName != null && x.TradeName.Contains(q))), ct);
            matchingCustomerIds = matchingCustomers.Select(x => x.Id).ToArray();
        }

        var page = await repository.GetPageAsync(specs.CreatePageSpecification(normalized, matchingCustomerIds), ct);
        var items = new List<PrescriptionDto>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var full = await repository.GetAggregateAsync(item.Id, false, ct) ?? item;
            items.Add(await assembler.PrescriptionAsync(full, ct));
        }
        return new PagedResult<PrescriptionDto> { Items = items, PageNumber = normalized.PageNumber, PageSize = normalized.PageSize, TotalCount = page.TotalCount };
    }
}
