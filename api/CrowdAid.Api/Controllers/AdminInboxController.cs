using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>Admin tabs for offers, volunteers and messages (spec #18–21). Needs an admin token.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route(ApiRoutes.Admin)]
public class AdminInboxController(IAdminInboxService inbox) : ControllerBase
{
    [HttpGet("offers")]
    public async Task<IActionResult> Offers([FromQuery] string? status, [FromQuery] string? campaignId, CancellationToken ct)
        => Ok(await inbox.ListOffersAsync(status, campaignId, ct));

    [HttpPatch("offers/{reference}")]
    public async Task<IActionResult> UpdateOffer(string reference, [FromBody] UpdateOfferRequest request, CancellationToken ct)
        => Ok(await inbox.UpdateOfferStatusAsync(reference, request, ct));

    [HttpGet("volunteers")]
    public async Task<IActionResult> Volunteers([FromQuery] string? outreachId, [FromQuery] string? kind, CancellationToken ct)
        => Ok(await inbox.ListVolunteersAsync(outreachId, kind, ct));

    [HttpGet("messages")]
    public async Task<IActionResult> Messages(CancellationToken ct)
        => Ok(await inbox.ListMessagesAsync(ct));
}
