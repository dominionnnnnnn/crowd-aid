using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrowdAid.Api.Controllers;

/// <summary>Healthcare assistance requests (spec #3, #34). Submitting never publishes anything.</summary>
[ApiController]
[AllowAnonymous]
[Route($"{ApiRoutes.Prefix}/applications")]
public class ApplicationsController(IApplicationService applications) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await applications.SubmitAsync(request, ct));

    [HttpGet("status")]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> Status([FromQuery] string? reference, [FromQuery] string? email, CancellationToken ct)
        => Ok(await applications.GetStatusAsync(reference, email, ct));
}
