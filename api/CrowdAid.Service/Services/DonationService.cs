using System.Text.Json;
using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Payments;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using CrowdAid.Service.Infrastructure;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrowdAid.Service.Services;

/// <summary>
/// Donations through Paystack test mode (spec #5, #6, #35).
/// raised/supporters only ever change through <see cref="ConfirmAsync"/>, which counts each payment once.
/// </summary>
public class DonationService(
    AppDbContext db,
    IReferenceGenerator refs,
    IPaystackClient paystack,
    IOptions<AppSettings> app,
    IOptions<PaystackSettings> paystackSettings,
    ILogger<DonationService> logger) : IDonationService
{
    // Paystack status -> CrowdAid status (spec #6 table)
    private static readonly HashSet<string> PendingStatuses = ["ongoing", "pending", "processing", "queued"];
    private static readonly HashSet<string> FailedStatuses = ["failed", "abandoned", "reversed"];

    public async Task<DonationStartedResponse> StartAsync(CreateDonationRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();

        var campaignId = r.CampaignId?.Trim() ?? "";
        if (r.Amount is not { } amount || amount < Lookups.MinimumDonation)
            v.Add("amount", $"Enter at least ₦{Lookups.MinimumDonation:N0}.");
        else if (amount > 100_000_000)
            v.Add("amount", "Enter a smaller amount.");
        var email = v.Email("email", r.Email);
        var method = v.OneOf("method", r.Method, PaymentMethod.All, "Choose how you'd like to pay.");
        var name = v.Optional("name", r.Name, 80);
        v.ThrowIfInvalid();

        var campaign = await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId && c.Published)
            .Select(c => new { c.Id, c.Title })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("This campaign isn't available.");

        var donation = new Donation
        {
            Reference = await refs.NextAsync(ReferencePrefix.Donation, ct),
            CampaignId = campaign.Id,
            Amount = r.Amount!.Value,
            PlatformFee = 0,
            Total = r.Amount!.Value, // Prototype v2: no processing fee shown to the donor
            Name = r.Anonymous == true ? null : name,
            Anonymous = r.Anonymous == true,
            Email = email!,
            Method = method,
            Status = DonationStatus.Pending,
            TestMode = true,
        };
        db.Donations.Add(donation);
        await db.SaveChangesAsync(ct);

        PaystackInitializeResult started;
        try
        {
            started = await paystack.InitializeAsync(new PaystackInitializeRequest(
                donation.Email,
                donation.Total,
                donation.Reference,
                $"{CallbackBase()}?ref={Uri.EscapeDataString(donation.Reference)}",
                PaymentMethod.ToPaystackChannel(method),
                campaign.Id), ct);
        }
        catch (Exception ex) when (ex is UpstreamException or HttpRequestException or TaskCanceledException)
        {
            donation.Status = DonationStatus.Failed;
            donation.GatewayStatus = "initialize_failed";
            await db.SaveChangesAsync(CancellationToken.None);
            if (ex is UpstreamException) throw;
            logger.LogWarning(ex, "Paystack initialize error for {Reference}", donation.Reference);
            throw new UpstreamException("PAYMENT_START_FAILED", "We couldn't start the payment. Please try again.");
        }

        return new DonationStartedResponse(
            donation.Reference, started.AuthorizationUrl, started.AccessCode,
            donation.Amount, donation.PlatformFee, donation.Total, donation.Status, donation.TestMode);
    }

    public async Task<DonationStatusResponse> GetStatusAsync(string reference, CancellationToken ct = default)
    {
        var refValue = reference.Trim().ToUpperInvariant();
        var donation = await db.Donations.AsNoTracking().FirstOrDefaultAsync(d => d.Reference == refValue, ct)
            ?? throw new NotFoundException("We couldn't find that donation.");

        // Still pending: ask Paystack. Never show success just because the donor came back.
        if (donation.Status == DonationStatus.Pending)
        {
            try
            {
                var tx = await paystack.VerifyAsync(donation.Reference, ct);
                await ApplyGatewayResultAsync(donation, tx, ct);
            }
            catch (Exception ex) when (ex is UpstreamException or HttpRequestException or TaskCanceledException)
            {
                logger.LogInformation("Verify not available yet for {Reference}: {Message}", donation.Reference, ex.Message);
            }
        }

        return await BuildStatusAsync(donation.Id, ct);
    }

    public async Task HandleWebhookAsync(string rawBody, string? signature, CancellationToken ct = default)
    {
        if (!paystack.IsValidSignature(rawBody, signature))
            throw new UnauthenticatedException("INVALID_SIGNATURE", "Invalid signature.");

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        if (root.GetProperty("event").GetString() != "charge.success") return; // ignore other events

        var tx = PaystackClient.ParseTransaction(root.GetProperty("data"));
        var donation = await db.Donations.AsNoTracking().FirstOrDefaultAsync(d => d.Reference == tx.Reference, ct);
        if (donation is null)
        {
            logger.LogWarning("Webhook for unknown reference {Reference}", tx.Reference);
            return;
        }

        await ApplyGatewayResultAsync(donation, tx, ct);
    }

    private async Task ApplyGatewayResultAsync(Donation donation, PaystackTransaction tx, CancellationToken ct)
    {
        var status = tx.Status.ToLowerInvariant();

        if (status == "success")
        {
            if (tx.AmountKobo != donation.Total * 100 || !string.Equals(tx.Currency, "NGN", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Amount/currency mismatch for {Reference}: {Amount} {Currency}",
                    donation.Reference, tx.AmountKobo, tx.Currency);
                return; // leave pending; an admin can look into it
            }
            await ConfirmAsync(donation, tx.PaidAt ?? DateTime.UtcNow, ct);
        }
        else if (FailedStatuses.Contains(status))
        {
            await db.Donations
                .Where(d => d.Id == donation.Id && d.Status == DonationStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, DonationStatus.Failed)
                    .SetProperty(d => d.GatewayStatus, status), ct);
        }
        else if (PendingStatuses.Contains(status))
        {
            await db.Donations
                .Where(d => d.Id == donation.Id && d.Status == DonationStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.GatewayStatus, status), ct);
        }
    }

    /// <summary>
    /// The confirm rule (spec #6): mark success only if not already success, and add to the
    /// campaign's totals only when that update actually changed a row. The verify call and the
    /// webhook both fire for the same payment, so this is what stops double counting.
    /// </summary>
    private async Task ConfirmAsync(Donation donation, DateTime paidAt, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var changed = await db.Donations
            .Where(d => d.Id == donation.Id && d.Status != DonationStatus.Success)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, DonationStatus.Success)
                .SetProperty(d => d.GatewayStatus, "success")
                .SetProperty(d => d.PaidAt, DateTime.SpecifyKind(paidAt, DateTimeKind.Utc)), ct);

        if (changed == 1)
        {
            await db.Campaigns
                .Where(c => c.Id == donation.CampaignId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Raised, c => c.Raised + donation.Amount)
                    .SetProperty(c => c.Supporters, c => c.Supporters + 1), ct);
        }

        await transaction.CommitAsync(ct);
    }

    private async Task<DonationStatusResponse> BuildStatusAsync(Guid donationId, CancellationToken ct)
    {
        return await db.Donations.AsNoTracking()
            .Where(d => d.Id == donationId)
            .Select(d => new DonationStatusResponse(
                d.Reference, d.CampaignId, d.Campaign.Title, d.Amount, d.Status, d.TestMode,
                new CampaignTotalsDto(d.Campaign.Raised, d.Campaign.Goal, d.Campaign.Supporters)))
            .FirstAsync(ct);
    }

    private string CallbackBase()
    {
        var configured = paystackSettings.Value.CallbackUrl;
        return !string.IsNullOrWhiteSpace(configured)
            ? configured.TrimEnd('/')
            : $"{app.Value.BaseUrl.TrimEnd('/')}/donate/result";
    }
}
