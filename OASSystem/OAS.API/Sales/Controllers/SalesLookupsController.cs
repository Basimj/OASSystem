using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Lookups;
using OAS.Contracts.Sales.Lookups;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/lookups")]
public sealed class SalesLookupsController(ISender sender) : ControllerBase
{
    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<SalesCustomerLookupDto>>> Customers(
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new SearchSalesCustomersQuery(search, take), cancellationToken));

    [HttpGet("prescriptions")]
    public async Task<ActionResult<IReadOnlyList<SalesPrescriptionLookupDto>>> Prescriptions(
        [FromQuery] Guid? customerId,
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new SearchSalesPrescriptionsQuery(customerId, search, take), cancellationToken));

    [HttpGet("prescriptions/{prescriptionId:guid}/revisions")]
    public async Task<ActionResult<IReadOnlyList<SalesPrescriptionRevisionLookupDto>>> PrescriptionRevisions(
        Guid prescriptionId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSalesPrescriptionRevisionsQuery(prescriptionId), cancellationToken));

    [HttpGet("product-variants")]
    public async Task<ActionResult<IReadOnlyList<SalesProductVariantLookupDto>>> ProductVariants(
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new SearchSalesProductVariantsQuery(search, take), cancellationToken));

    [HttpGet("warehouses")]
    public async Task<ActionResult<IReadOnlyList<SalesWarehouseLookupDto>>> Warehouses(
        [FromQuery] Guid? productVariantId,
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new SearchSalesWarehousesQuery(productVariantId, search, take), cancellationToken));

    [HttpGet("currencies")]
    public async Task<ActionResult<IReadOnlyList<SalesCurrencyLookupDto>>> Currencies(
        [FromQuery] DateOnly documentDate,
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new SearchSalesCurrenciesQuery(documentDate, search, take), cancellationToken));

    [HttpGet("customer-orders")]
    public async Task<ActionResult<IReadOnlyList<CustomerOrderLookupDto>>> CustomerOrders(
        [FromQuery] Guid? customerId,
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new SearchCustomerOrdersLookupQuery(customerId, search, take), cancellationToken));
}
