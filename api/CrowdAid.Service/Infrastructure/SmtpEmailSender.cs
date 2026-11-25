using System.Net.Mail;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrowdAid.Service.Infrastructure;

/// <summary>
/// Sends over plain SMTP. In the demo, Smtp__Host=mailhog: MailHog catches everything and
/// delivers nothing. Moving to a real provider later only changes Smtp__* settings.
/// </summary>
public class SmtpEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _s = options.Value;

    public async Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_s.Enabled) return false;
        try
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(_s.From),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody,
            };
            msg.To.Add(new MailAddress(to));

            using var smtp = new SmtpClient(_s.Host, _s.Port) { EnableSsl = false };
            await smtp.SendMailAsync(msg, ct);
            return true;
        }
        catch (Exception ex)
        {
            // Never log the body: it can contain an activation link.
            logger.LogWarning(ex, "Email to {Domain} failed", to.Split('@').LastOrDefault());
            return false;
        }
    }
}
