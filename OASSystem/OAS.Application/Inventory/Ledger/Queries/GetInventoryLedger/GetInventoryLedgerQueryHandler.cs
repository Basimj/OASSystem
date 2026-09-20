using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Inventory.Ledger.Mapping;
using OAS.Application.Inventory.Ledger.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Ledger.Queries.GetInventoryLedger;

public sealed class GetInventoryLedgerQueryHandler(
    IReadRepository<InventoryLedger, Guid> repository,
    InventoryLedgerMapper mapper)
    : IRequestHandler<GetInventoryLedgerQuery, PagedResult<InventoryLedgerDto>>
{
    public async Task<PagedResult<InventoryLedgerDto>> Handle(
        GetInventoryLedgerQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = InventoryLedgerSpecification.Create(
            normalized,
            request.WarehouseId,
            request.ProductVariantId,
            request.TransactionId,
            request.MovementType,
            request.FromDate,
            request.ToDate);

        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<InventoryLedgerDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
