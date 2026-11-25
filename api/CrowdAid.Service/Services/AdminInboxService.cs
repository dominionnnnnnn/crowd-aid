using System.Globalization;
using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>Offers, volunteers and messages tabs for admins (spec #18–21). Full contact details; admins only.</summary>
public class AdminInboxService(AppDbContext db) : IAdminInboxService
{
    public async Task<ListResponse<AdminOfferRowDto>> ListOffersAsync(string? status, string? campaignId, CancellationToken ct = default)
    {
        var query = db.Offers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(campaignId)) query = query.Where(o => o.CampaignId == campaignId.Trim());

        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOfferRowDto(
                o.Reference, o.Name, o.Phone, o.Types,
                o.Campaign != null ? o.Campaign.Title : "General (no campaign)",
                o.Availability, o.Area, o.Details, o.Status, o.CreatedAt,
                o.Status == OfferStatus.New))
            .ToListAsync(ct);

        return new ListResponse<AdminOfferRowDto>(rows);
    }

    public async Task<AdminOfferRowDto> UpdateOfferStatusAsync(string reference, UpdateOfferRequest request, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var status = v.OneOf("status", request.Status, OfferStatus.All, "Choose a valid offer status.");
        v.ThrowIfInvalid();

        var refValue = reference.Trim().ToUpperInvariant();
        var offer = await db.Offers.Include(o => o.Campaign).FirstOrDefaultAsync(o => o.Reference == refValue, ct)
            ?? throw new NotFoundException("Offer not found.");

        offer.Status = status;
        await db.SaveChangesAsync(ct);

        return new AdminOfferRowDto(
            offer.Reference, offer.Name, offer.Phone, offer.Types,
            offer.Campaign?.Title ?? "General (no campaign)",
            offer.Availability, offer.Area, offer.Details, offer.Status, offer.CreatedAt,
            offer.Status == OfferStatus.New);
    }

    public async Task<ListResponse<AdminVolunteerRowDto>> ListVolunteersAsync(string? outreachId, string? kind, CancellationToken ct = default)
    {
        var query = db.Volunteers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(v => v.Kind == kind.Trim());
        if (!string.IsNullOrWhiteSpace(outreachId)) query = query.Where(v => v.OutreachId == outreachId.Trim());

        var raw = await query
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new
            {
                v.Reference, v.Kind, v.Name, v.Phone, v.Area, v.Roles, v.Stay, v.Availability, v.CreatedAt,
                OutreachTitle = v.Outreach != null ? v.Outreach.Title : null,
                OutreachDate = v.Outreach != null ? (DateOnly?)v.Outreach.Date : null,
            })
            .ToListAsync(ct);

        var since = DateTime.UtcNow.AddHours(-24);
        var rows = raw.Select(v =>
        {
            var isOutreach = v.Kind == VolunteerKind.Outreach;
            var label = isOutreach && v.OutreachTitle is not null && v.OutreachDate is { } date
                ? $"{v.OutreachTitle} · {date.ToString("d MMM", CultureInfo.InvariantCulture)}"
                : "Volunteer community (ongoing)";
            object availability = isOutreach
                ? v.Stay switch { "all" => "All of it", "part" => "Part of it", _ => "" }
                : v.Availability;
            return new AdminVolunteerRowDto(v.Reference, v.Kind, v.Name, v.Phone, label, v.Roles,
                availability, v.Area, v.CreatedAt, v.CreatedAt > since);
        }).ToList();

        return new ListResponse<AdminVolunteerRowDto>(rows);
    }

    public async Task<ListResponse<AdminMessageRowDto>> ListMessagesAsync(CancellationToken ct = default)
    {
        var rows = await db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new AdminMessageRowDto(
                m.Reference, m.Name, m.Phone,
                m.Campaign != null ? m.Campaign.Title : "General enquiry",
                m.Message, m.CreatedAt))
            .ToListAsync(ct);

        return new ListResponse<AdminMessageRowDto>(rows);
    }
}
