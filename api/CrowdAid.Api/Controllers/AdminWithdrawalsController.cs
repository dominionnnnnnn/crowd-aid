using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>Admin withdrawals tab (spec #31–32). Demo only: no money moves. Needs an admin token.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route($"{ApiRoutes.Admin}/withdrawals")]
public class AdminWithdrawalsController(IAdminWithdrawalService withdrawals) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await withdrawals.ListAsync(status, ct));

    [HttpPost("{reference}/actions")]
    public async Task<IActionResult> Action(string reference, [FromBody] WithdrawalActionRequest request, CancellationToken ct)
        => Ok(await withdrawals.ActionAsync(reference, request, User.DisplayName(), User.UserId(), ct));
}
