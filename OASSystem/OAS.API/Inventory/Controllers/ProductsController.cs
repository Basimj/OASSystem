using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Application.Inventory.Products.Products.Commands.UpdateProduct;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Products;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/products")]
public sealed class ProductsController(
    ICrudApplicationService<Guid, ProductDto, CreateProductRequest, UpdateProductRequest> service,
    IPermissionChecker permissionChecker,
    ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Products.View, cancellationToken);
        return Ok(await service.GetPageAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Products.View, cancellationToken);
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Products.Create, cancellationToken);

        // Keep the legacy endpoint safe: stock products must go through the integrated
        // workflow so the final state always contains a valid initial variant.
        var created = await sender.Send(
            new CreateStockProductCommand(new CreateStockProductRequest(request, null, null)),
            cancellationToken);
        var result = await service.GetByIdAsync(created.ProductId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }


    [HttpPost("stock-product")]
    public async Task<ActionResult<CreateStockProductResult>> CreateStockProduct(
        [FromBody] CreateStockProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateStockProductCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.ProductId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UpdateProductCommand(id, request), cancellationToken));
    }

    private async Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken)
    {
        if (!await permissionChecker.HasPermissionAsync(permission, cancellationToken))
            throw new ForbiddenException();
    }
}
