using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Contracts.Bootstrap;

namespace OAS.API.Bootstrap.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/bootstrap")]
public sealed class BootstrapController(IConfiguration configuration) : ControllerBase
{
    private const string FallbackSystemName = "OAS";
    private const string FallbackCulture = "ar";

    [HttpGet]
    [ProducesResponseType(typeof(BootstrapInfoDto), StatusCodes.Status200OK)]
    public ActionResult<BootstrapInfoDto> Get()
    {
        var systemName = configuration["Application:Name"]?.Trim();
        var defaultCulture = NormalizeCulture(configuration["Localization:DefaultCulture"]);

        return Ok(new BootstrapInfoDto(
            string.IsNullOrWhiteSpace(systemName) ? FallbackSystemName : systemName,
            defaultCulture));
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return FallbackCulture;

        return culture.Trim().ToLowerInvariant() switch
        {
            "ar" or "ar-ye" => "ar",
            "en" or "en-us" or "en-gb" => "en",
            _ => FallbackCulture
        };
    }
}
