namespace CrowdAid.Data.DTOs.Admin;

// GET /admin/offers (#18). Private contact details, admins only.
public record AdminOfferRowDto(
    string Reference,
    string Name,
    string Phone,
    List<string> Types,
    string CampaignTitle,
    List<string> Availability,
    string Area,
    string? Details,
    string Status,
    DateTime CreatedAt,
    bool IsNew);

// PATCH /admin/offers/:ref (#19)
public record UpdateOfferRequest(string? Status);

/// <summary>GET /admin/volunteers (#20). availability is a string for outreach sign-ups, a list for community.</summary>
public record AdminVolunteerRowDto(
    string Reference,
    string Kind,
    string Name,
    string Phone,
    string OutreachLabel,
    List<string> RoleOrInterests,
    object Availability,
    string Area,
    DateTime CreatedAt,
    bool IsNew);

// GET /admin/messages (#21)
public record AdminMessageRowDto(
    string Reference,
    string Name,
    string Phone,
    string About,
    string Message,
    DateTime CreatedAt);
