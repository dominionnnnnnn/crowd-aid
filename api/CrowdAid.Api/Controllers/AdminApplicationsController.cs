using CrowdAid.Api.Common;
using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>Admin review of healthcare applications (spec #13–17, #33). Needs an admin token.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route($"{ApiRoutes.Admin}/applications")]
public class AdminApplicationsController(IAdminApplicationService applications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await applications.ListAsync(status, ct));

    [HttpGet("{reference}")]
    public async Task<IActionResult> Get(string reference, CancellationToken ct)
        => Ok(await applications.GetAsync(reference, User.DisplayName(), ct));

    [HttpPatch("{reference}/verification")]
    public async Task<IActionResult> UpdateVerification(string reference, [FromBody] VerificationRequest request, CancellationToken ct)
        => Ok(await applications.UpdateVerificationAsync(reference, request, User.DisplayName(), ct));

    [HttpPost("{reference}/actions")]
    public async Task<IActionResult> Action(string reference, [FromBody] ApplicationActionRequest request, CancellationToken ct)
        => Ok(await applications.ActionAsync(reference, request, User.DisplayName(), User.UserId(), ct));

    [HttpPost("{reference}/publish")]
    public async Task<IActionResult> Publish(string reference, [FromBody] PublishRequest request, CancellationToken ct)
        => Ok(await applications.PublishAsync(reference, request, User.DisplayName(), ct));

    [HttpPost("{reference}/activation")]
    public async Task<IActionResult> ResendActivation(string reference, CancellationToken ct)
        => Ok(await applications.ResendActivationAsync(reference, User.DisplayName(), User.UserId(), ct));
}
