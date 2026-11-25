using System.Text.RegularExpressions;
using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.DTOs.Me;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

public partial class MeService(AppDbContext db, IReferenceGenerator refs) : IMeService
{
    // ---- #26 ----

    public async Task<MeCampaignResponse> GetCampaignAsync(Guid userId, CancellationToken ct = default)
    {
        var (app, campaign) = await ResolveAsync(userId, ct);
        var payoutSet = await db.PayoutDetails.AnyAsync(p => p.UserId == userId, ct);
        var application = new MeApplicationDto(app.Reference, app.Status, app.PatientName);

        if (campaign is null) return new MeCampaignResponse(application, null, payoutSet);

        var money = await BalanceAsync(campaign, ct);
        var updates = await db.CampaignUpdates.AsNoTracking()
            .Where(u => u.CampaignId == campaign.Id)
            .OrderByDescending(u => u.Date)
            .Select(u => new CampaignUpdateDto(u.Date, u.Title, u.Text))
            .ToListAsync(ct);

        return new MeCampaignResponse(application, new MeCampaignDto(
            campaign.Id, campaign.Title, campaign.Published, campaign.Raised, campaign.Goal, campaign.Supporters,
            money.Withdrawn, money.Pending, money.Available, campaign.Needs, updates), payoutSet);
    }

    // ---- #27 ----

    public async Task<PagedResponse<MeDonationDto>> ListDonationsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (_, campaign) = await ResolveAsync(userId, ct);
        if (campaign is null) return new PagedResponse<MeDonationDto>([], 0);

        // Confirmed donations only. Email, phone and payment method are never selected.
        var query = db.Donations.AsNoTracking()
            .Where(d => d.CampaignId == campaign.Id && d.Status == DonationStatus.Success);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(d => d.PaidAt ?? d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new { d.Reference, d.Name, d.Anonymous, d.Amount, When = d.PaidAt ?? d.CreatedAt })
            .ToListAsync(ct);

        var data = rows.Select(d => new MeDonationDto(d.Reference, DisplayName(d.Name, d.Anonymous), d.Amount, d.When)).ToList();
        return new PagedResponse<MeDonationDto>(data, total);
    }

    // ---- #28 ----

    public async Task<ListResponse<MeOfferDto>> ListOffersAsync(Guid userId, string? status, CancellationToken ct = default)
    {
        var (_, campaign) = await ResolveAsync(userId, ct);
        if (campaign is null) return new ListResponse<MeOfferDto>([]);

        // Offers for this campaign only. General offers (no campaign) are not included.
        var query = db.Offers.AsNoTracking().Where(o => o.CampaignId == campaign.Id);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status.Trim());

        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new MeOfferDto(o.Reference, o.Types, o.Details, o.Availability, o.Area, o.Status, o.CreatedAt))
            .ToListAsync(ct);

        return new ListResponse<MeOfferDto>(rows);
    }

    // ---- #29 ----

    public async Task<MaskedPayoutDto?> GetPayoutAsync(Guid userId, CancellationToken ct = default)
    {
        var p = await db.PayoutDetails.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p is null ? null : Mask(p);
    }

    public async Task<MaskedPayoutDto> SavePayoutAsync(Guid userId, PayoutRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var bank = v.Required("bankName", r.BankName, "This field is required.", 80);
        var accountName = v.Required("accountName", r.AccountName, "This field is required.", 120);
        var number = r.AccountNumber?.Trim() ?? "";
        if (!TenDigits().IsMatch(number)) v.Add("accountNumber", "Enter exactly 10 digits.");
        v.ThrowIfInvalid();

        // Sensitive: never logged.
        var p = await db.PayoutDetails.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (p is null)
        {
            p = new PayoutDetails { UserId = userId };
            db.PayoutDetails.Add(p);
        }
        p.BankName = bank;
        p.AccountNumber = number;
        p.AccountName = accountName;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Mask(p);
    }

    // ---- #30 ----

    public async Task<MeWithdrawalsResponse> ListWithdrawalsAsync(Guid userId, CancellationToken ct = default)
    {
        var (_, campaign) = await ResolveAsync(userId, ct);
        if (campaign is null) return new MeWithdrawalsResponse([], 0);

        var rows = await db.Withdrawals.AsNoTracking()
            .Where(w => w.CampaignId == campaign.Id && w.UserId == userId)
            .OrderByDescending(w => w.RequestedAt)
            .Select(w => new MeWithdrawalDto(w.Reference, w.Amount, w.Status, w.RequestedAt,
                w.DecidedAt ?? w.PaidAt, w.Note))
            .ToListAsync(ct);

        var money = await BalanceAsync(campaign, ct);
        return new MeWithdrawalsResponse(rows, money.Available);
    }

    public async Task<WithdrawalCreatedResponse> RequestWithdrawalAsync(Guid userId, CreateWithdrawalRequest r, CancellationToken ct = default)
    {
        var (_, campaign) = await ResolveAsync(userId, ct);
        if (campaign is null || !campaign.Published)
            throw new ConflictException("CAMPAIGN_NOT_PUBLISHED",
                "Your campaign must be published before requesting a withdrawal.");

        if (!await db.PayoutDetails.AnyAsync(p => p.UserId == userId, ct))
            throw new ConflictException("PAYOUT_DETAILS_REQUIRED", "Add payout details first.");

        if (r.Amount is not { } amount || amount < Lookups.MinimumWithdrawal)
            throw ValidationFailedException.For("amount",
                $"Enter a whole naira amount of at least ₦{Lookups.MinimumWithdrawal:N0}.");

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // One request at a time per campaign: two quick clicks can't spend the same money twice.
        var key = campaign.Id;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({key}))", ct);

        var fresh = await db.Campaigns.AsNoTracking().FirstAsync(c => c.Id == campaign.Id, ct);
        var money = await BalanceAsync(fresh, ct);
        if (amount > money.Available)
            throw new ConflictException("INSUFFICIENT_FUNDS", "This amount is more than your available balance.");

        var withdrawal = new Withdrawal
        {
            Reference = await refs.NextAsync(ReferencePrefix.Withdrawal, ct),
            UserId = userId,
            CampaignId = campaign.Id,
            Amount = amount,
            Status = WithdrawalStatus.Requested,
            Demo = true,
        };
        db.Withdrawals.Add(withdrawal);
        db.WithdrawalLogs.Add(new WithdrawalLog { WithdrawalId = withdrawal.Id, Action = "Requested", By = user.Name });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new WithdrawalCreatedResponse(withdrawal.Reference, amount, withdrawal.Status,
            money.Available - amount, true);
    }

    // ---- helpers ----

    /// <summary>
    /// The signed-in user's most recent approved application and its campaign (null before publish).
    /// Everything on /me/* starts here, from the user id in the token only.
    /// </summary>
    private async Task<(Application App, Campaign? Campaign)> ResolveAsync(Guid userId, CancellationToken ct)
    {
        var app = await db.Applications.AsNoTracking()
            .Where(a => a.UserId == userId &&
                        (a.Status == ApplicationStatus.Approved || a.Status == ApplicationStatus.Published))
            .OrderByDescending(a => a.ReceivedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("We couldn't find an approved application for this account.");

        Campaign? campaign = null;
        if (app.CampaignId is { } campaignId)
            campaign = await db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        return (app, campaign);
    }

    /// <summary>available = raised − withdrawn (Paid) − pending (Requested + Approved). Always calculated, never stored.</summary>
    private async Task<(long Withdrawn, long Pending, long Available)> BalanceAsync(Campaign campaign, CancellationToken ct)
    {
        var sums = await db.Withdrawals.AsNoTracking()
            .Where(w => w.CampaignId == campaign.Id)
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Total = g.Sum(w => w.Amount) })
            .ToListAsync(ct);

        long Sum(params string[] statuses) => sums.Where(s => statuses.Contains(s.Status)).Sum(s => s.Total);
        var withdrawn = Sum(WithdrawalStatus.Paid);
        var pending = Sum(WithdrawalStatus.Requested, WithdrawalStatus.Approved);
        return (withdrawn, pending, campaign.Raised - withdrawn - pending);
    }

    /// <summary>"Tunde Bakare" -> "Tunde B."; anonymous or blank -> "Anonymous supporter".</summary>
    private static string DisplayName(string? name, bool anonymous)
    {
        if (anonymous || string.IsNullOrWhiteSpace(name)) return "Anonymous supporter";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? parts[0] : $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}.";
    }

    private static MaskedPayoutDto Mask(PayoutDetails p)
        => new(p.BankName, "••••••" + p.AccountNumber[^Math.Min(4, p.AccountNumber.Length)..], p.AccountName);

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex TenDigits();
}
