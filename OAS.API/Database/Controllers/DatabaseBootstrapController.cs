using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Contracts.Database;
using OAS.Infrastructure.Database.Services;

namespace OAS.API.Database.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("database-bootstrap")]
[Route("api/database/bootstrap")]
public sealed class DatabaseBootstrapController(DatabaseMaintenanceService service) : ControllerBase
{
    [HttpGet("profiles")]
    public ActionResult<DatabaseProfilesResponse> Profiles() => Ok(service.GetProfiles());

    [HttpGet("status")]
    public async Task<ActionResult<DatabaseUpdateStatusDto>> Status([FromQuery] string? profileKey, CancellationToken cancellationToken) =>
        Ok(await service.GetStatusAsync(profileKey, cancellationToken));

    [HttpPost("update")]
    public async Task<ActionResult<DatabaseUpdateStatusDto>> Update(DatabaseUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(request.ProfileKey, cancellationToken));
}
