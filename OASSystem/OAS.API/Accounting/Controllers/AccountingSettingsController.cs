using MediatR;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.RateLimiting;using OAS.Application.Accounting.Settings.Commands.UpdateAccountingSettings;using OAS.Application.Accounting.Settings.Queries.GetAccountingSettings;using OAS.Contracts.Accounting.Settings;
namespace OAS.API.Accounting.Controllers;
[ApiController,Authorize,EnableRateLimiting("api"),Route("api/accounting/accounting-settings")]
public sealed class AccountingSettingsController(ISender sender):ControllerBase
{
 [HttpGet] public async Task<ActionResult<AccountingSettingsDto?>> Get(CancellationToken ct)=>Ok(await sender.Send(new GetAccountingSettingsQuery(),ct));
 [HttpPut] public async Task<ActionResult<AccountingSettingsDto?>> Update(UpdateAccountingSettingsRequest request,CancellationToken ct){await sender.Send(new UpdateAccountingSettingsCommand(request),ct);return Ok(await sender.Send(new GetAccountingSettingsQuery(),ct));}
}
