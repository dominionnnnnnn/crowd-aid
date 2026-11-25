using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>
/// Approve, reject or mark paid (spec #31–32). Demo only: "paid" changes a status, nothing is transferred.
/// A reject frees the money again because the campaigner's available balance is always calculated.
/// </summary>
public class AdminWithdrawalService(AppDbContext db) : IAdminWithdrawalService
{
    private static ConflictException NotAllowed() => new("INVALID_STATUS",
        "This action is not allowed in the current status. Refresh and try again.");

    public async Task<ListResponse<AdminWithdrawalRowDto>> ListAsync(string? status, CancellationToken ct = default)
    {
        var query = db.Withdrawals.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(w => w.Status == status.Trim());

        var rows = await query
            .OrderByDescending(w => w.RequestedAt)
            .Select(w => new
            {
                w.Reference, w.Amount, w.Status, w.RequestedAt,
                CampaignTitle = w.Campaign.Title,
                ApplicantName = w.User.Name,
                w.UserId,
            })
            .ToListAsync(ct);

        // Payout details in a second query: simpler for EF than a correlated subquery.
        var userIds = rows.Select(w => w.UserId).Distinct().ToList();
        var payouts = await db.PayoutDetails.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var data = rows.Select(w => new AdminWithdrawalRowDto(
            w.Reference, w.CampaignTitle, w.ApplicantName, w.Amount, w.Status, w.RequestedAt,
            w.Status == WithdrawalStatus.Requested,
            payouts.TryGetValue(w.UserId, out var p)
                ? new PayoutDto(p.BankName, p.AccountNumber, p.AccountName)
                : new PayoutDto("", "", "")))
            .ToList();

        return new ListResponse<AdminWithdrawalRowDto>(data);
    }

    public async Task<WithdrawalActionResponse> ActionAsync(
        string reference, WithdrawalActionRequest r, string adminName, Guid adminId, CancellationToken ct = default)
    {
        var action = r.Action?.Trim();
        var v = new FormValidator();
        var note = v.Optional("note", r.Note, 1000);
        string? reason = null, message = null, transferReference = null;

        switch (action)
        {
            case "reject":
                reason = v.Required("reason", r.Reason, "This field is required.", 1000);
                message = v.Required("message", r.Message, "This field is required.", 2000);
                break;
            case "mark_paid":
                transferReference = v.Optional("transferReference", r.TransferReference, 100);
                break;
            case "approve":
                break;
            default:
                v.Add("action", "Choose a valid action.");
                break;
        }
        v.ThrowIfInvalid();

        var refValue = reference.Trim().ToUpperInvariant();
        var withdrawal = await db.Withdrawals.FirstOrDefaultAsync(w => w.Reference == refValue, ct)
            ?? throw new NotFoundException("Withdrawal not found.");

        string[] allowedFrom;
        string to, logAction;
        switch (action)
        {
            case "approve":
                allowedFrom = [WithdrawalStatus.Requested];
                to = logAction = WithdrawalStatus.Approved;
                break;
            case "reject":
                allowedFrom = [WithdrawalStatus.Requested, WithdrawalStatus.Approved];
                to = logAction = WithdrawalStatus.Rejected;
                break;
            default: // mark_paid
                allowedFrom = [WithdrawalStatus.Approved];
                to = WithdrawalStatus.Paid;
                logAction = "Marked paid";
                break;
        }

        if (!allowedFrom.Contains(withdrawal.Status)) throw NotAllowed();

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Guarded update: two admins clicking at once can't both succeed.
        var moved = await db.Withdrawals
            .Where(w => w.Id == withdrawal.Id && allowedFrom.Contains(w.Status))
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.Status, to), ct);
        if (moved == 0) throw NotAllowed();

        withdrawal.Status = to;
        if (action == "mark_paid")
        {
            withdrawal.PaidAt = now;
            withdrawal.TransferReference = transferReference;
        }
        else
        {
            withdrawal.DecidedBy = adminId;
            withdrawal.DecidedAt = now;
            if (action == "reject")
            {
                withdrawal.Reason = reason;
                withdrawal.Note = message; // shown to the campaigner
            }
        }

        db.WithdrawalLogs.Add(new WithdrawalLog
        {
            WithdrawalId = withdrawal.Id,
            Action = logAction,
            By = adminName,
            Note = action == "reject" ? reason : note ?? transferReference,
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new WithdrawalActionResponse(withdrawal.Reference, to);
    }
}
