namespace CrowdAid.Data.DTOs.Public;

// POST /donations (spec #5)
public record CreateDonationRequest(
    string? CampaignId,
    long? Amount,
    string? Name,
    bool? Anonymous,
    string? Email,
    string? Method);

public record DonationStartedResponse(
    string Reference,
    string AuthorizationUrl,
    string AccessCode,
    long Amount,
    long PlatformFee,
    long Total,
    string Status,
    bool TestMode);

// GET /donations/:ref (spec #6). No donor name or email.
public record DonationStatusResponse(
    string Reference,
    string CampaignId,
    string CampaignTitle,
    long Amount,
    string Status,
    bool TestMode,
    CampaignTotalsDto Campaign);
