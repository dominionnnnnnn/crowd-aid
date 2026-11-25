using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrowdAid.Data.DTOs.Payments;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrowdAid.Service.Infrastructure;

/// <summary>Paystack test mode (spec #5, #6, #35). Uses the secret key server-side only.</summary>
public class PaystackClient(HttpClient http, IOptions<PaystackSettings> options, ILogger<PaystackClient> logger)
    : IPaystackClient
{
    private readonly PaystackSettings _s = options.Value;

    public async Task<PaystackInitializeResult> InitializeAsync(PaystackInitializeRequest r, CancellationToken ct = default)
    {
        var body = new
        {
            email = r.Email,
            amount = r.AmountNaira * 100, // Paystack takes kobo
            currency = "NGN",
            reference = r.Reference,
            callback_url = r.CallbackUrl,
            channels = new[] { r.Channel },
            metadata = new { campaignId = r.CampaignId },
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "/transaction/initialize")
        {
            Content = JsonContent.Create(body),
        };
        Authorize(req);

        using var res = await http.SendAsync(req, ct);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!res.IsSuccessStatusCode || !json.GetProperty("status").GetBoolean())
        {
            logger.LogWarning("Paystack initialize failed for {Reference}: {Status}", r.Reference, (int)res.StatusCode);
            throw new UpstreamException("PAYMENT_START_FAILED", "We couldn't start the payment. Please try again.");
        }

        var data = json.GetProperty("data");
        return new PaystackInitializeResult(
            data.GetProperty("authorization_url").GetString()!,
            data.GetProperty("access_code").GetString()!,
            data.GetProperty("reference").GetString()!);
    }

    public async Task<PaystackTransaction> VerifyAsync(string reference, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/transaction/verify/{Uri.EscapeDataString(reference)}");
        Authorize(req);

        using var res = await http.SendAsync(req, ct);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!res.IsSuccessStatusCode || !json.GetProperty("status").GetBoolean())
            throw new UpstreamException("PAYMENT_VERIFY_FAILED", "We couldn't confirm the payment yet. Please check again.");

        return ParseTransaction(json.GetProperty("data"));
    }

    /// <summary>Parses the "data" object of a verify response or a charge.success webhook event.</summary>
    public static PaystackTransaction ParseTransaction(JsonElement data)
    {
        DateTime? paidAt = data.TryGetProperty("paid_at", out var p) && p.ValueKind == JsonValueKind.String
            && DateTime.TryParse(p.GetString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var d)
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;

        return new PaystackTransaction(
            data.GetProperty("reference").GetString()!,
            data.GetProperty("status").GetString()!,
            data.GetProperty("amount").GetInt64(),
            data.GetProperty("currency").GetString()!,
            paidAt);
    }

    public bool IsValidSignature(string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrEmpty(_s.SecretKey)) return false;

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_s.SecretKey));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signatureHeader.Trim().ToLowerInvariant()));
    }

    private void Authorize(HttpRequestMessage req)
    {
        if (string.IsNullOrEmpty(_s.SecretKey))
            throw new UpstreamException("PAYMENT_NOT_CONFIGURED", "Payments aren't set up on this server yet.");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _s.SecretKey);
    }
}
