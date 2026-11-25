using CrowdAid.Api.Common;
using CrowdAid.Data.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdAid.Api.Controllers;

/// <summary>
/// Paystack webhook (spec #35). The signature is an HMAC of the exact bytes Paystack sent,
/// so the body is read raw here and never bound to a model.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route($"{ApiRoutes.Prefix}/webhooks/paystack")]
public class PaystackWebhookController(IDonationService donations) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
        await donations.HandleWebhookAsync(rawBody, signature, ct);
        return Ok();
    }
}
