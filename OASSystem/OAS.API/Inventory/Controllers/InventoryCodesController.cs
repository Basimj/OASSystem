using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Services;
using OAS.Contracts.Inventory;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/codes")]
public sealed class InventoryCodesController(
    IInventoryCodeGenerator codeGenerator,
    IPermissionChecker permissionChecker) : ControllerBase
{
    [HttpGet("next/{kind}")]
    public async Task<ActionResult<InventoryCodeSuggestionDto>> Next(
        string kind,
        CancellationToken cancellationToken)
    {
        var permission = ResolvePermission(kind);
        if (!await permissionChecker.HasPermissionAsync(permission, cancellationToken))
            throw new ForbiddenException();

        var code = await codeGenerator.NextAsync(kind, cancellationToken);
        return Ok(new InventoryCodeSuggestionDto(kind, code));
    }

    private static string ResolvePermission(string kind) =>
        kind.Trim().ToLowerInvariant() switch
        {
            InventoryCodeKinds.Product => InventoryPermissions.Products.Create,
            InventoryCodeKinds.Brand => InventoryPermissions.Brands.Create,
            InventoryCodeKinds.ProductCategory => InventoryPermissions.ProductCategories.Create,
            InventoryCodeKinds.ProductType => InventoryPermissions.Products.Create,
            InventoryCodeKinds.Unit => InventoryPermissions.Units.Create,
            InventoryCodeKinds.Warehouse => InventoryPermissions.Warehouses.Create,
            InventoryCodeKinds.ProductVariant => InventoryPermissions.ProductVariants.Create,
            _ => throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["kind"] = ["inventory_code_kind_invalid: نوع كود المخزون غير صالح."]
            })
        };
}
