using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.CustomerOrders.Commands;
using OAS.Application.Sales.CustomerOrders.Queries;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/customer-orders")]
public sealed class CustomerOrdersController(
    ISender sender,
    ILensVariantResolver lensVariants,
    ISalesPrescriptionValidator prescriptionValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerOrderDto>>> Get([FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrdersQuery(request), cancellationToken));

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<CustomerOrdersDashboardSummaryDto>> DashboardSummary(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrdersDashboardSummaryQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken));

    [HttpPost("code/reserve")]
    public async Task<ActionResult<SalesCodeReservationDto>> ReserveCode([FromQuery] DateOnly orderDate, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReserveCustomerOrderCodeCommand(orderDate), cancellationToken));

    // This is a read-only preflight: it never saves an order or changes inventory.
    // A missing SKU is a normal validation result, not a thrown ConflictException.
    [HttpPost("optical/prevalidate")]
    public async Task<ActionResult<CustomerOrderOpticalPrevalidationResult>> PrevalidateOptical(
        [FromBody] CustomerOrderOpticalPrevalidationRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count > 200)
            return BadRequest("يجب إرسال قائمة بنود صالحة لا تتجاوز 200 بند للتحقق المسبق.");

        var issues = new List<CustomerOrderOpticalPrevalidationIssue>();
        var lineNumber = 0;
        foreach (var line in request.Lines)
        {
            lineNumber++;
            if (line.LineType != SalesLineType.Lens || !line.ProductVariantId.HasValue ||
                line.ProductVariantId.Value == Guid.Empty || line.OpticalSnapshot is null)
                continue;

            decimal? sph;
            decimal? cyl;
            decimal? add;
            var snapshot = line.OpticalSnapshot;
            if (snapshot?.MeasurementSource == OpticalMeasurementSource.Manual)
            {
                sph = snapshot.SPH;
                cyl = snapshot.CYL;
                add = snapshot.ADD;
                if (!sph.HasValue || !cyl.HasValue)
                {
                    issues.Add(new(lineNumber, "sales_optical_manual_power_required",
                        "في الإدخال اليدوي، أدخل SPH وCYL لكل عين (ضع CYL = 0 عند عدم وجود استجماتيزم)."));
                    continue;
                }
                if (cyl.Value != 0m && (snapshot.Axis is null or < 1 or > 180))
                {
                    issues.Add(new(lineNumber, "sales_optical_manual_axis_required",
                        "عندما تكون CYL غير صفر، يجب تحديد Axis من 1 إلى 180."));
                    continue;
                }
                if (snapshot.Axis is < 0 or > 180 || snapshot.ADD is < 0 || snapshot.Prism is < 0 ||
                    snapshot.PD is <= 0 || snapshot.MonocularPD is <= 0 || snapshot.FittingHeight is <= 0)
                {
                    issues.Add(new(lineNumber, "sales_optical_manual_measurements_invalid",
                        "راجع قيم Axis وADD والقياسات الإضافية؛ توجد قيمة خارج النطاق المسموح."));
                    continue;
                }
            }
            else
            {
                var revisionId = snapshot?.PrescriptionRevisionId
                    ?? line.PrescriptionRevisionId ?? request.PrescriptionRevisionId;
                var eye = snapshot?.Eye ?? line.PrescriptionEye;
                if (!revisionId.HasValue || !eye.HasValue)
                    continue; // Existing UI validation reports the missing prescription/eye.

                var detail = await prescriptionValidator.ValidateLineAsync(
                    revisionId, (OAS.Domain.Sales.Enums.EyeSide)(byte)eye.Value, true, cancellationToken);
                if (detail is null)
                    continue;
                sph = detail.SPH;
                cyl = detail.CYL;
                add = detail.ADD;
            }

            var exact = await lensVariants.TryResolveExactAsync(
                new LensVariantMatchRequest(line.ProductVariantId.Value, sph, cyl, add), cancellationToken);
            if (exact is null)
            {
                static string Format(decimal? value) => value?.ToString("0.##") ?? "غير محدد";
                issues.Add(new(lineNumber, "sales_lens_variant_not_defined",
                    $"لا يوجد SKU عدسة مطابق للصنف المحدد بقياسات SPH={Format(sph)}، CYL={Format(cyl)}، ADD={Format(add)}. " +
                    "اختر صنف عدسة له هذه القياسات، أو اطلب تعريفه ضمن أصناف العدسات في المخزون أولًا. " +
                    "لا يتطلب هذا إدخال PD أو Prism أو VA لإيجاد SKU مطابق."));
            }
        }

        return Ok(new CustomerOrderOpticalPrevalidationResult(issues.Count == 0, issues));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerOrderDto>> Create([FromBody] CreateCustomerOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateCustomerOrderCommand(request), cancellationToken);
        var dto = await sender.Send(new GetCustomerOrderByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerOrderDto>> Update(Guid id, [FromBody] UpdateCustomerOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCustomerOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/header-discount")]
    public async Task<ActionResult<CustomerOrderDto>> SetHeaderDiscount(Guid id, [FromBody] SetCustomerOrderHeaderDiscountRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetCustomerOrderHeaderDiscountCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/header-discount/approve")]
    public async Task<ActionResult<CustomerOrderDto>> ApproveHeaderDiscount(Guid id, [FromBody] ApproveCustomerOrderHeaderDiscountRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveCustomerOrderHeaderDiscountCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<CustomerOrderDto>> Confirm(Guid id, [FromBody] ConfirmCustomerOrderRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ConfirmCustomerOrderCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CustomerOrderDto>> Cancel(Guid id, [FromBody] CancelCustomerOrderRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new CancelCustomerOrderCommand(id, request), cancellationToken));
}
