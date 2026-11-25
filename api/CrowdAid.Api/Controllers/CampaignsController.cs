using CrowdAid.Api.Common;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>Public campaign browsing (spec #1, #2).</summary>
[ApiController]
[AllowAnonymous]
[Route($"{ApiRoutes.Prefix}/campaigns")]
public class CampaignsController(ICampaignService campaigns) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] string? location,
        [FromQuery] string? need, [FromQuery] bool? featured, CancellationToken ct)
        => Ok(await campaigns.ListAsync(q, location, need, featured, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => Ok(await campaigns.GetPublishedAsync(id, ct));
}
