using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Identity.Appearance;
using OAS.Contracts.Identity.Appearance;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/profile/appearance")]
public sealed class AppearanceController(IAppearancePreferencesService appearance) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AppearanceSettingsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await appearance.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<AppearanceSettingsDto>> Put([FromBody] AppearanceSettingsDto settings, CancellationToken cancellationToken)
    {
        try { return Ok(await appearance.SaveAsync(settings, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await appearance.ResetAsync(cancellationToken);
        return NoContent();
    }
}
