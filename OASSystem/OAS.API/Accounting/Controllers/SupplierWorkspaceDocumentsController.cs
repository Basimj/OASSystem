using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Reports.Queries.GetSupplierWorkspaceDocuments;
using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/suppliers/{supplierId:guid}/workspace-documents")]
public sealed class SupplierWorkspaceDocumentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SupplierWorkspaceDocumentsDto>> Get(
        Guid supplierId,
        [FromQuery] string documentType,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] string? search = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetSupplierWorkspaceDocumentsQuery(
            new SupplierWorkspaceDocumentsFilter(supplierId, documentType, fromDate, toDate,
                search, currencyCode, pageNumber, pageSize)), cancellationToken));
}
