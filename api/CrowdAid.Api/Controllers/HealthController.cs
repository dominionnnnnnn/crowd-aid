using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>
/// Scaffold checks. /api/health is used by deploy.sh and the README; /api/hello by the
/// placeholder page in web/src/App.jsx. Both stay outside the /api/v1 prefix.
/// </summary>
[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", time = DateTime.UtcNow });

    [HttpGet("hello")]
    public IActionResult Hello() => Ok(new { id = 1, message = "Hello from the CrowdAid API 👋", servedAt = DateTime.UtcNow });
}
