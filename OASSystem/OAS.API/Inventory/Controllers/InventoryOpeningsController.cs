using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Abstractions.Security;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Transactions.Openings;
using OAS.Contracts.Inventory.Transactions;
using System.Globalization;

namespace OAS.API.Inventory.Controllers;

[ApiController, Authorize, EnableRateLimiting("api")]
[Route("api/inventory/openings")]
public sealed class InventoryOpeningsController(ISender sender, IPermissionChecker permissions) : ControllerBase
{
    [HttpGet("info")]
    public Task<InventoryOpeningInfo> Info(CancellationToken ct) => sender.Send(new GetInventoryOpeningInfo(), ct);
    [HttpPost]
    public Task<Guid> Create(InventoryOpeningRequest request, CancellationToken ct) => sender.Send(new SaveInventoryOpening(null, request), ct);
    [HttpPut("{id:guid}")]
    public Task<Guid> Update(Guid id, InventoryOpeningRequest request, CancellationToken ct) => sender.Send(new SaveInventoryOpening(id, request), ct);
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await sender.Send(new DeleteInventoryOpening(id), ct); return NoContent(); }
    [HttpPost("{id:guid}/post")]
    public Task<Guid> Post(Guid id, CancellationToken ct) => sender.Send(new PostInventoryOpening(id), ct);

    [HttpPost("{id:guid}/reverse")]
    public Task<Guid> Reverse(Guid id, ReverseInventoryOpeningRequest request, CancellationToken ct) => sender.Send(new ReverseInventoryOpening(id, request), ct);

    [HttpGet("template")]
    public async Task<IActionResult> Template(CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(InventoryPermissions.Openings.Edit, ct)) return Forbid();
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet("Opening");
        var headers = new[] {"SKU", "Quantity", "UnitCost", "Notes"};
        for (var i=0; i<headers.Length; i++) sheet.Cell(1,i+1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(1).Style.NumberFormat.Format = "@";
        sheet.Columns(1,4).Width = 24;
        using var output = new MemoryStream(); book.SaveAs(output);
        return File(output.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory-opening.xlsx");
    }
    [HttpPost("preview"), RequestSizeLimit(6*1024*1024)]
    public async Task<ActionResult<IReadOnlyList<InventoryOpeningImportRow>>> Preview(IFormFile file, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(InventoryPermissions.Openings.Edit, ct)) return Forbid();
        if (file is null || file.Length == 0 || file.Length > 5*1024*1024 || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "اختر ملف Excel صالحًا بحجم لا يتجاوز 5 MB." });
        try
        {
            using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct); stream.Position = 0;
            using var book = new XLWorkbook(stream); var sheet = book.Worksheet(1);
            var expected = new[] {"SKU", "Quantity", "UnitCost", "Notes"};
            if (expected.Where((x,i) => sheet.Cell(1,i+1).GetString().Trim() != x).Any()) return BadRequest(new { message = "استخدم قالب الرصيد الافتتاحي." });
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > 5001) return BadRequest(new { message = "الحد الأقصى 5000 بند." });
            var rows = new List<InventoryOpeningImportRow>();
            for (var i=2; i<=last; i++)
            {
                if (Enumerable.Range(1,4).Any(c => sheet.Cell(i,c).HasFormula)) return BadRequest(new { message = $"لا تستخدم صيغ Excel في السطر {i}." });
                var sku = sheet.Cell(i,1).GetString().Trim();
                if (Enumerable.Range(1,4).All(c => sheet.Cell(i,c).IsEmpty())) continue;
                if (sku.Length == 0 || !sheet.Cell(i,2).TryGetValue<decimal>(out var qty) || !sheet.Cell(i,3).TryGetValue<decimal>(out var cost) || qty <= 0 || cost <= 0)
                    return BadRequest(new { message = $"بيانات السطر {i} غير صالحة." });
                rows.Add(new(sku,qty,cost,sheet.Cell(i,4).GetString()));
            }
            return Ok(rows);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { return BadRequest(new { message = "تعذر قراءة ملف Excel. تحقق من القالب والبيانات." }); }
    }
}
