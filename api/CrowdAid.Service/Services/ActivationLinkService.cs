using System.Net;
using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.Entities;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrowdAid.Service.Services;

public record IssuedLink(string Link, DateTime ExpiresAt);

/// <summary>
/// Creates one-time activation links and emails them (spec #16 approve, #33 resend).
/// Only the SHA-256 hash is stored. The raw link lives in memory for one response.
/// </summary>
public class ActivationLinkService(
    AppDbContext db,
    IActivationTokenFactory tokens,
    IEmailSender email,
    IOptions<AppSettings> app)
{
    /// <summary>
    /// Revokes the user's earlier live links and adds a new token to the context.
    /// The caller saves (inside its transaction) and sends the email only after the commit.
    /// </summary>
    public async Task<IssuedLink> CreateAsync(User user, Guid? createdBy, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.ActivationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

        var raw = tokens.CreateRawToken();
        var expires = now.Add(Lookups.ActivationLinkLifetime);
        db.ActivationTokens.Add(new ActivationToken
        {
            UserId = user.Id,
            TokenHash = tokens.Hash(raw),
            ExpiresAt = expires,
            CreatedBy = createdBy,
        });

        return new IssuedLink($"{app.Value.BaseUrl.TrimEnd('/')}/activate?token={raw}", expires);
    }

    /// <summary>Sends the activation email. Returns false when SMTP is off or fails; the approval still stands.</summary>
    public Task<bool> SendAsync(User user, Application application, IssuedLink link, CancellationToken ct)
    {
        var name = WebUtility.HtmlEncode(user.Name);
        var reference = WebUtility.HtmlEncode(application.Reference);
        var url = WebUtility.HtmlEncode(link.Link);

        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#1b2b24">
              <h2 style="color:#157347">Set up your CrowdAid account</h2>
              <p>Hello {name},</p>
              <p>Your application <strong>{reference}</strong> was approved. Set a password to sign in and follow your campaign.</p>
              <p><a href="{url}" style="background:#157347;color:#fff;padding:12px 20px;border-radius:8px;text-decoration:none;display:inline-block">Set up my account</a></p>
              <p>Or copy this link into your browser:<br><a href="{url}">{url}</a></p>
              <p>This link works once and expires in 72 hours.</p>
              <hr style="border:none;border-top:1px solid #dde5e0">
              <p style="font-size:12px;color:#5c6b64">Demo email: sent to the CrowdAid demo inbox, not delivered.</p>
            </div>
            """;

        return email.SendAsync(user.Email, "Set up your CrowdAid account", html, ct);
    }
}
