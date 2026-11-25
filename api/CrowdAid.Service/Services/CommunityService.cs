using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>Practical help offers, outreach, volunteers and contact messages (spec #7–#11).</summary>
public class CommunityService(AppDbContext db, IReferenceGenerator refs) : ICommunityService
{
    private const string AreaMessage = "Choose your area in Abeokuta.";
    private const string NameMessage = "Enter your full name.";

    public async Task<OfferCreatedResponse> CreateOfferAsync(CreateOfferRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var types = v.Many("types", r.Types, Lookups.HelpCategories, "Choose at least one way you can help.");
        var details = v.Optional("details", r.Details, 500);
        var availability = v.Many("availability", r.Availability, Lookups.Availability, "Choose when you're available.");
        var area = v.OneOf("area", r.Area, Lookups.AbeokutaAreas, AreaMessage);
        var name = v.Required("name", r.Name, NameMessage, 120);
        var phone = v.Phone("phone", r.Phone);
        v.MustBeTrue("contactConsent", r.ContactConsent, "Tick this box so CrowdAid can contact you about this offer.");

        var campaign = await FindPublishedCampaignAsync(r.CampaignId, v, ct);
        v.ThrowIfInvalid();

        var offer = new Offer
        {
            Reference = await refs.NextAsync(ReferencePrefix.Offer, ct),
            CampaignId = campaign?.Id,
            Types = types,
            Details = details,
            Availability = availability,
            Area = area,
            Name = name,
            Phone = phone,
            ContactConsent = true,
            Status = OfferStatus.New,
        };
        db.Offers.Add(offer);
        await db.SaveChangesAsync(ct);

        return campaign is null
            ? new OfferCreatedResponse(offer.Reference, "general", null)
            : new OfferCreatedResponse(offer.Reference, "campaign", campaign.Title);
    }

    public async Task<ListResponse<OutreachDto>> ListOutreachAsync(CancellationToken ct = default)
    {
        var today = LagosToday();
        var items = await db.Outreaches.AsNoTracking()
            .Where(o => o.Date >= today)
            .OrderBy(o => o.Date)
            .Select(o => new OutreachDto(
                o.Id, o.Title, o.Date, o.Time, o.Venue,
                o.Campaign != null && o.Campaign.Published ? new CampaignRefDto(o.Campaign.Id, o.Campaign.Title) : null))
            .ToListAsync(ct);

        return new ListResponse<OutreachDto>(items);
    }

    public async Task<OutreachVolunteerResponse> RegisterForOutreachAsync(OutreachVolunteerRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var outreachId = r.OutreachId?.Trim() ?? "";
        var today = LagosToday();
        var outreach = outreachId.Length == 0 ? null : await db.Outreaches.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == outreachId && o.Date >= today, ct);
        if (outreach is null) v.Add("outreachId", "Choose the outreach you want to join.");

        var name = v.Required("name", r.Name, NameMessage, 120);
        var phone = v.Phone("phone", r.Phone);
        var area = v.OneOf("area", r.Area, Lookups.AbeokutaAreas, AreaMessage);
        var roles = v.Many("roles", r.Roles, Lookups.OutreachRoles, "Choose from the listed roles.", atLeastOne: false);
        var stay = v.OneOf("stay", r.Stay, Lookups.OutreachStay, "Tell us if you can stay for the whole outreach.");
        v.MustBeTrue("guidelinesConsent", r.GuidelinesConsent, "Tick this box to agree to CrowdAid's volunteer guidelines.");
        v.ThrowIfInvalid();

        var volunteer = new Volunteer
        {
            Reference = await refs.NextAsync(ReferencePrefix.Volunteer, ct),
            Kind = VolunteerKind.Outreach,
            OutreachId = outreach!.Id,
            Name = name,
            Phone = phone,
            Area = area,
            Roles = roles,
            Stay = stay,
            Consent = true,
        };
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync(ct);

        return new OutreachVolunteerResponse(volunteer.Reference,
            new OutreachSummaryDto(outreach.Title, outreach.Date, outreach.Time));
    }

    public async Task<ReferenceResponse> JoinCommunityAsync(CommunityVolunteerRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var name = v.Required("name", r.Name, NameMessage, 120);
        var phone = v.Phone("phone", r.Phone);
        var email = v.Email("email", r.Email, required: false);
        var area = v.OneOf("area", r.Area, Lookups.AbeokutaAreas, AreaMessage);
        var interests = v.Many("interests", r.Interests, Lookups.CommunityInterests, "Choose at least one way you'd like to help.");
        var availability = v.Many("availability", r.Availability, Lookups.Availability, "Choose when you're available.");
        v.MustBeTrue("contactConsent", r.ContactConsent, "Tick this box so CrowdAid can contact you about volunteering.");
        v.ThrowIfInvalid();

        var volunteer = new Volunteer
        {
            Reference = await refs.NextAsync(ReferencePrefix.Volunteer, ct),
            Kind = VolunteerKind.Community,
            Name = name,
            Phone = phone,
            Email = email,
            Area = area,
            Roles = interests,
            Availability = availability,
            Consent = true,
        };
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync(ct);

        return new ReferenceResponse(volunteer.Reference);
    }

    public async Task<ReferenceResponse> CreateContactMessageAsync(CreateContactMessageRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var name = v.Required("name", r.Name, NameMessage, 120);
        var phone = v.Phone("phone", r.Phone);
        var message = v.Required("message", r.Message, "Write your message.", 1000);
        var campaign = await FindPublishedCampaignAsync(r.CampaignId, v, ct);
        v.ThrowIfInvalid();

        var msg = new ContactMessage
        {
            Reference = await refs.NextAsync(ReferencePrefix.Message, ct),
            CampaignId = campaign?.Id,
            Name = name,
            Phone = phone,
            Message = message,
        };
        db.ContactMessages.Add(msg);
        await db.SaveChangesAsync(ct);

        return new ReferenceResponse(msg.Reference);
    }

    /// <summary>Null when no campaign id was sent (general mode). Adds a field error when the id is unknown.</summary>
    private async Task<CampaignRefDto?> FindPublishedCampaignAsync(string? campaignId, FormValidator v, CancellationToken ct)
    {
        var id = campaignId?.Trim();
        if (string.IsNullOrEmpty(id)) return null;

        var campaign = await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == id && c.Published)
            .Select(c => new CampaignRefDto(c.Id, c.Title))
            .FirstOrDefaultAsync(ct);
        if (campaign is null) v.Add("campaignId", "This campaign isn't available.");
        return campaign;
    }

    /// <summary>Today's date in Nigeria (UTC+1, no daylight saving).</summary>
    private static DateOnly LagosToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(1));
}
