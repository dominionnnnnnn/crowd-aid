using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Auth;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrowdAid.Api.Controllers;

/// <summary>Sign-in, activation and session restore (spec #12, #22–25).</summary>
[ApiController]
[Route(ApiRoutes.Prefix)]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("admin/auth/login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await auth.AdminLoginAsync(request, ct));

    [HttpPost("auth/login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await auth.CampaignerLoginAsync(request, ct));

    [HttpPost("auth/activation/check")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> CheckActivation([FromBody] ActivationCheckRequest request, CancellationToken ct)
        => Ok(await auth.CheckActivationAsync(request, ct));

    [HttpPost("auth/activate")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> Activate([FromBody] ActivateRequest request, CancellationToken ct)
        => Ok(await auth.ActivateAsync(request, ct));

    /// <summary>Any valid token (admin or campaigner). Used to restore the session after a refresh.</summary>
    [HttpGet("auth/me")]
    [Authorize(Policy = Policies.AnySignedIn)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await auth.GetMeAsync(User.UserId(), ct));
}
