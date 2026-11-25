using CrowdAid.Data.DTOs.Public;

namespace CrowdAid.Data.DTOs.Me;

// GET /me/campaign (#26)
public record MeApplicationDto(string Reference, string Status, string PatientName);

public record MeCampaignDto(
    string Id,
    string Title,
    bool Published,
    long Raised,
    long Goal,
    int Supporters,
    long Withdrawn,
    long PendingWithdrawals,
    long Available,
    List<string> Needs,
    List<CampaignUpdateDto> Updates);

/// <summary>campaign is null until the application is published.</summary>
public record MeCampaignResponse(MeApplicationDto Application, MeCampaignDto? Campaign, bool PayoutSet);

// GET /me/campaign/donations (#27). Never contains email, phone or payment method.
public record MeDonationDto(string Reference, string DisplayName, long Amount, DateTime CreatedAt);

// GET /me/campaign/offers (#28). No name or phone: those stay with admins.
public record MeOfferDto(
    string Reference,
    List<string> Types,
    string? Details,
    List<string> Availability,
    string Area,
    string Status,
    DateTime CreatedAt);

// GET and PUT /me/payout (#29)
public record PayoutRequest(string? BankName, string? AccountNumber, string? AccountName);

/// <summary>accountNumber is masked, e.g. "••••••6789".</summary>
public record MaskedPayoutDto(string BankName, string AccountNumber, string AccountName);

// GET and POST /me/withdrawals (#30)
public record MeWithdrawalDto(
    string Reference, long Amount, string Status, DateTime RequestedAt, DateTime? DecidedAt, string? Note);

public record MeWithdrawalsResponse(IReadOnlyList<MeWithdrawalDto> Data, long Available);

public record CreateWithdrawalRequest(long? Amount);

public record WithdrawalCreatedResponse(string Reference, long Amount, string Status, long Available, bool Demo);
