using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Me;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>
/// Campaigner dashboard (spec #26–30). Needs a campaigner token. There are no ids in these URLs:
/// the campaign is always the signed-in user's own.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.Campaigner)]
[Route(ApiRoutes.Me)]
public class MeController(IMeService me) : ControllerBase
{
    [HttpGet("campaign")]
    public async Task<IActionResult> Campaign(CancellationToken ct)
        => Ok(await me.GetCampaignAsync(User.UserId(), ct));

    [HttpGet("campaign/donations")]
    public async Task<IActionResult> Donations([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await me.ListDonationsAsync(User.UserId(), page, pageSize, ct));

    [HttpGet("campaign/offers")]
    public async Task<IActionResult> Offers([FromQuery] string? status, CancellationToken ct)
        => Ok(await me.ListOffersAsync(User.UserId(), status, ct));

    /// <summary>Returns the masked details, or JSON null when none are saved yet.</summary>
    [HttpGet("payout")]
    public async Task<IActionResult> GetPayout(CancellationToken ct)
    {
        var payout = await me.GetPayoutAsync(User.UserId(), ct);
        return payout is null ? Content("null", "application/json") : Ok(payout);
    }

    [HttpPut("payout")]
    public async Task<IActionResult> SavePayout([FromBody] PayoutRequest request, CancellationToken ct)
        => Ok(await me.SavePayoutAsync(User.UserId(), request, ct));

    [HttpGet("withdrawals")]
    public async Task<IActionResult> Withdrawals(CancellationToken ct)
        => Ok(await me.ListWithdrawalsAsync(User.UserId(), ct));

    [HttpPost("withdrawals")]
    public async Task<IActionResult> RequestWithdrawal([FromBody] CreateWithdrawalRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await me.RequestWithdrawalAsync(User.UserId(), request, ct));
}
