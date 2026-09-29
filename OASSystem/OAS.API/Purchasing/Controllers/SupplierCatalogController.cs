using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.SupplierCatalog.Commands;
using OAS.Application.Purchasing.SupplierCatalog.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.SupplierCatalog;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/supplier-catalog")]
public sealed class SupplierCatalogController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierCatalogItemDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new GetSupplierCatalogQuery(request, supplierId, productVariantId, isActive),
            cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierCatalogItemDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSupplierCatalogItemByIdQuery(id), cancellationToken));

    [HttpGet("product/{productVariantId:guid}")]
    public async Task<ActionResult<PagedResult<SupplierCatalogItemDto>>> GetByProduct(
        Guid productVariantId,
        [FromQuery] PageRequest request,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new GetSupplierCatalogQuery(request, ProductVariantId: productVariantId, IsActive: isActive),
            cancellationToken));

    [HttpGet("supplier/{supplierId:guid}")]
    public async Task<ActionResult<PagedResult<SupplierCatalogItemDto>>> GetBySupplier(
        Guid supplierId,
        [FromQuery] PageRequest request,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new GetSupplierCatalogQuery(request, SupplierId: supplierId, IsActive: isActive),
            cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SupplierCatalogItemDto>> Create(
        [FromBody] CreateSupplierCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateSupplierCatalogItemCommand(request), cancellationToken);
        var dto = await sender.Send(new GetSupplierCatalogItemByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierCatalogItemDto>> Update(
        Guid id,
        [FromBody] UpdateSupplierCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateSupplierCatalogItemCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetSupplierCatalogItemByIdQuery(id), cancellationToken));
    }

    [HttpPost("{catalogItemId:guid}/prices")]
    public async Task<ActionResult<SupplierCatalogItemDto>> AddPrice(
        Guid catalogItemId,
        [FromBody] CreateSupplierPriceRequest request,
        CancellationToken cancellationToken)
    {
        _ = await sender.Send(new CreateSupplierPriceCommand(catalogItemId, request), cancellationToken);
        var dto = await sender.Send(new GetSupplierCatalogItemByIdQuery(catalogItemId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = catalogItemId }, dto);
    }

    [HttpPost("{catalogItemId:guid}/prices/{priceId:guid}/close")]
    public async Task<ActionResult<SupplierCatalogItemDto>> ClosePrice(
        Guid catalogItemId,
        Guid priceId,
        [FromBody] CloseSupplierPriceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CloseSupplierPriceCommand(catalogItemId, priceId, request), cancellationToken);
        return Ok(await sender.Send(new GetSupplierCatalogItemByIdQuery(catalogItemId), cancellationToken));
    }
}
