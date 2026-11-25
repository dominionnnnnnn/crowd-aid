using System.Net.Mail;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CrowdAid.Api.Controllers;

/// <summary>
/// POST /api/test-email: scaffold check that SMTP reaches MailHog.
/// Only works while Smtp__Host is "mailhog", so it can never send real mail.
/// DELETE this controller before switching to a real email provider.
/// </summary>
[ApiController]
[Route("api")]
public class DevController(IEmailSender email, IOptions<SmtpSettings> smtp, IOptions<AppSettings> app) : ControllerBase
{
    public record TestEmailDto(string? To);

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(TestEmailDto dto, CancellationToken ct)
    {
        if (!smtp.Value.Host.Equals("mailhog", StringComparison.OrdinalIgnoreCase)) return NotFound();

        if (!MailAddress.TryCreate(dto.To ?? "", out var to))
            return BadRequest(ErrorResponse.Of("VALIDATION_ERROR", "Please fix the highlighted fields.",
                new Dictionary<string, string> { ["to"] = "Enter a valid email." }));

        var link = $"{app.Value.BaseUrl.TrimEnd('/')}/activate?token=TEST-ONLY";
        var sent = await email.SendAsync(to.Address, "CrowdAid: set up your account (test)",
            $"<p>Hello,</p><p>Your CrowdAid application was approved. Set your password here:</p>" +
            $"<p><a href=\"{link}\">{link}</a></p><p>This link works once and expires in 72 hours.</p>" +
            "<p><em>Demo email: sent to the CrowdAid demo inbox, not delivered.</em></p>", ct);

        return Ok(new { sent, to = to.Address });
    }
}
