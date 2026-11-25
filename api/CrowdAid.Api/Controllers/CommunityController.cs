using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrowdAid.Api.Controllers;

/// <summary>Practical help, outreach, volunteers and contact messages (spec #7–#11).</summary>
[ApiController]
[AllowAnonymous]
[Route(ApiRoutes.Prefix)]
public class CommunityController(ICommunityService community) : ControllerBase
{
    [HttpPost("practical-help-offers")]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await community.CreateOfferAsync(request, ct));

    [HttpGet("outreach")]
    public async Task<IActionResult> ListOutreach(CancellationToken ct)
        => Ok(await community.ListOutreachAsync(ct));

    [HttpPost("volunteers/outreach")]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> RegisterForOutreach([FromBody] OutreachVolunteerRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await community.RegisterForOutreachAsync(request, ct));

    [HttpPost("volunteers/community")]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> JoinCommunity([FromBody] CommunityVolunteerRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await community.JoinCommunityAsync(request, ct));

    [HttpPost("contact-messages")]
    [EnableRateLimiting(RateLimits.PublicForms)]
    public async Task<IActionResult> CreateContactMessage([FromBody] CreateContactMessageRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await community.CreateContactMessageAsync(request, ct));
}
