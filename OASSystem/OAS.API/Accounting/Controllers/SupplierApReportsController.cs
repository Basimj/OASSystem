using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Reports.Queries.GetApReconciliation;
using OAS.Application.Accounting.Reports.Queries.GetSupplierBalances;
using OAS.Application.Accounting.Reports.Queries.GetSupplierStatement;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/reports/suppliers")]
public sealed class SupplierApReportsController(ISender sender) : ControllerBase
{
    [HttpGet("{supplierId:guid}/statement")]
    public async Task<ActionResult<SupplierStatementDto>> Statement(
        Guid supplierId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] string? reference = null,
        [FromQuery] string? sourceDocumentType = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new GetSupplierStatementQuery(
                supplierId,
                fromDate,
                toDate,
                reference,
                sourceDocumentType,
                currencyCode,
                pageNumber,
                pageSize,
                sortBy,
                sortDirection),
            cancellationToken));

    [HttpGet("balances")]
    public async Task<ActionResult<SupplierBalancesDto>> Balances(
        [FromQuery] DateOnly asOfDate,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] SortDirection sortDirection = SortDirection.Ascending,
        [FromQuery] bool includeZeroBalance = false,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new GetSupplierBalancesQuery(
                asOfDate,
                search,
                pageNumber,
                pageSize,
                sortBy,
                sortDirection,
                includeZeroBalance),
            cancellationToken));

    [HttpGet("reconciliation")]
    public async Task<ActionResult<ApReconciliationDto>> Reconciliation(
        [FromQuery] DateOnly asOfDate,
        [FromQuery] Guid? controlAccountId = null,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new GetApReconciliationQuery(asOfDate, controlAccountId),
            cancellationToken));
}
