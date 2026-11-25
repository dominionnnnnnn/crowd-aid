namespace CrowdAid.Data.DTOs.Admin;

// GET /admin/applications (#13)
public record AdminApplicationRowDto(
    string Reference,
    string Applicant,
    string Patient,
    string Relationship,
    string Town,
    string Treatment,
    long? AmountNeeded,
    string Status,
    bool HasEvidence,
    DateTime ReceivedAt,
    bool IsNew);

public record VerificationDto(string Status, string Method);

public record ApplicationLogDto(string Action, string By, DateTime At, string? Note);

public record PublicationDto(
    string Title,
    string Location,
    long Goal,
    string? Organizer,
    string Story,
    string ConsentRecorded,
    bool PublicationConsent,
    bool ContentReviewed);

/// <summary>Account state shown on the application detail (spec #14).</summary>
public record AccountSummaryDto(string State, DateTime? ActivationExpiresAt, DateTime? ActivatedAt);

// GET /admin/applications/:ref (#14). Private: full phone and email, admins only.
public record AdminApplicationDetailDto(
    string Reference,
    DateTime ReceivedAt,
    string Who,
    string Name,
    string Phone,
    string Email,
    string Relationship,
    string PatientName,
    int PatientAge,
    string Town,
    string Lga,
    string Condition,
    string Treatment,
    string? Facility,
    long? EstimatedCost,
    long? AmountNeeded,
    List<string> SupportTypes,
    bool PrivateReviewConsent,
    bool AccuracyConfirmed,
    string Status,
    VerificationDto Verification,
    List<string> Files,
    List<ApplicationLogDto> Log,
    PublicationDto? Publication,
    string? CampaignId,
    AccountSummaryDto Account);

// PATCH /admin/applications/:ref/verification (#15)
public record VerificationRequest(string? Status, string? Method);

// POST /admin/applications/:ref/actions (#16)
public record ApplicationActionRequest(string? Action, string? Needed, string? Message, string? Reason, string? Note);

/// <summary>
/// Returned by approve (#16) and resend (#33). activationLink is shown once, only to the admin:
/// never log it, never store it. emailSent is null when no link was created (already activated).
/// </summary>
public record AccountLinkDto(
    string State,
    string Email,
    string Phone,
    string? ActivationLink,
    DateTime? ExpiresAt,
    bool? EmailSent);

public record ApplicationActionResponse(string Reference, string Status, AccountLinkDto? Account);

// POST /admin/applications/:ref/publish (#17)
public record PublishRequest(
    string? Title,
    string? Location,
    long? Goal,
    string? Organizer,
    string? Story,
    string? ConsentRecorded,
    bool? PublicationConsent,
    bool? ContentReviewed);

public record PublishResponse(string CampaignId, string Status);
