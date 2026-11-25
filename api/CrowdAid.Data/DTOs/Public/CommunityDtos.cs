namespace CrowdAid.Data.DTOs.Public;

// POST /practical-help-offers (spec #7)
public record CreateOfferRequest(
    string? CampaignId,
    List<string>? Types,
    string? Details,
    List<string>? Availability,
    string? Area,
    string? Name,
    string? Phone,
    bool? ContactConsent);

/// <summary>mode = "campaign" | "general". campaignTitle is null in general mode.</summary>
public record OfferCreatedResponse(string Reference, string Mode, string? CampaignTitle);

// GET /outreach (spec #8)
public record OutreachDto(
    string Id,
    string Title,
    DateOnly Date,
    string Time,
    string Venue,
    CampaignRefDto? SupportingCampaign);

public record CampaignRefDto(string Id, string Title);

// POST /volunteers/outreach (spec #9)
public record OutreachVolunteerRequest(
    string? OutreachId,
    string? Name,
    string? Phone,
    string? Area,
    List<string>? Roles,
    string? Stay,
    bool? GuidelinesConsent);

public record OutreachSummaryDto(string Title, DateOnly Date, string Time);

public record OutreachVolunteerResponse(string Reference, OutreachSummaryDto Outreach);

// POST /volunteers/community (spec #10)
public record CommunityVolunteerRequest(
    string? Name,
    string? Phone,
    string? Email,
    string? Area,
    List<string>? Interests,
    List<string>? Availability,
    bool? ContactConsent);

// POST /contact-messages (spec #11)
public record CreateContactMessageRequest(string? CampaignId, string? Name, string? Phone, string? Message);

/// <summary>{ "reference": "CA-…" }</summary>
public record ReferenceResponse(string Reference);
