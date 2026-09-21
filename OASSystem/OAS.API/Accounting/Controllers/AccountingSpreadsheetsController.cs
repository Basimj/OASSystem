using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Spreadsheets;
namespace OAS.API.Accounting.Controllers;
[ApiController,Authorize,EnableRateLimiting("api")]
[Route("api/accounting/spreadsheets/{section}")]
public sealed class AccountingSpreadsheetsController(AccountingSpreadsheetService service):ControllerBase
{
    private const string Mime="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    [HttpGet("template")]
    public async Task<IActionResult> Template(string section,CancellationToken ct)=>File(await service.TemplateAsync(section,ct),Mime,$"{section}-template.xlsx");
    [HttpGet("export")]
    public async Task<IActionResult> Export(string section,[FromQuery] PageRequest request,[FromQuery] string? sourceType,CancellationToken ct)=>File(await service.ExportAsync(section,request,sourceType,ct),Mime,$"{section}.xlsx");
    [HttpPost("preview"),RequestSizeLimit(11*1024*1024)]
    public Task<ActionResult<SpreadsheetPreview>> Preview(string section,IFormFile file,CancellationToken ct)=>Upload(section,file,false,ct);
    [HttpPost("import"),RequestSizeLimit(11*1024*1024)]
    public Task<ActionResult<SpreadsheetPreview>> Import(string section,IFormFile file,CancellationToken ct)=>Upload(section,file,true,ct);
    private async Task<ActionResult<SpreadsheetPreview>> Upload(string section,IFormFile file,bool confirm,CancellationToken ct)
    {
        if(file is null || file.Length==0 || file.Length>10*1024*1024 || !file.FileName.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { code="invalid_file",message="اختر ملف xlsx غير فارغ بحجم لا يتجاوز 10 MB." });
        using var memory=new MemoryStream();await file.CopyToAsync(memory,ct);
        return Ok(confirm?await service.ImportAsync(section,memory.ToArray(),ct):await service.PreviewAsync(section,memory.ToArray(),ct));
    }
}
