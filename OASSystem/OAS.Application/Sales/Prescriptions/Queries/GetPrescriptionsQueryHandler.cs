using MediatR;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Prescriptions.Specifications;
using OAS.Application.Sales.Services;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Application.Sales.Prescriptions.Queries;

public sealed class GetPrescriptionsQueryHandler(IPrescriptionAggregateRepository repository, PrescriptionSpecificationFactory specs, SalesDtoAssembler assembler)
    : IRequestHandler<GetPrescriptionsQuery, PagedResult<PrescriptionDto>>
{
    public async Task<PagedResult<PrescriptionDto>> Handle(GetPrescriptionsQuery request, CancellationToken ct)
    {
        var normalized = request.Request.Normalize();
        var page = await repository.GetPageAsync(specs.CreatePageSpecification(normalized), ct);
        var items = new List<PrescriptionDto>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var full = await repository.GetAggregateAsync(item.Id, false, ct) ?? item;
            items.Add(await assembler.PrescriptionAsync(full, ct));
        }
        return new PagedResult<PrescriptionDto> { Items = items, PageNumber = normalized.PageNumber, PageSize = normalized.PageSize, TotalCount = page.TotalCount };
    }
}
