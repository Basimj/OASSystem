using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Products;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/product-variants")]
public sealed class ProductVariantsController(
    ICrudApplicationService<Guid, ProductVariantDto, CreateProductVariantRequest, UpdateProductVariantRequest> service,
    IPermissionChecker permissionChecker) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductVariantDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.ProductVariants.View, cancellationToken);
        return Ok(await service.GetPageAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductVariantDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.ProductVariants.View, cancellationToken);
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProductVariantDto>> Create(
        [FromBody] CreateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.ProductVariants.Create, cancellationToken);
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductVariantDto>> Update(
        Guid id,
        [FromBody] UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.ProductVariants.Edit, cancellationToken);
        return Ok(await service.UpdateAsync(id, request, cancellationToken));
    }

    private async Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken)
    {
        if (!await permissionChecker.HasPermissionAsync(permission, cancellationToken))
            throw new ForbiddenException();
    }
}
