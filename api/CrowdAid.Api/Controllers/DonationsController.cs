using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrowdAid.Api.Controllers;

/// <summary>Donations through Paystack test mode (spec #5, #6).</summary>
[ApiController]
[AllowAnonymous]
[Route($"{ApiRoutes.Prefix}/donations")]
public class DonationsController(IDonationService donations) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> Start([FromBody] CreateDonationRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await donations.StartAsync(request, ct));

    [HttpGet("{reference}")]
    public async Task<IActionResult> Get(string reference, CancellationToken ct)
        => Ok(await donations.GetStatusAsync(reference, ct));
}
