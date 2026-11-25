namespace CrowdAid.Data.DTOs.Admin;

/// <summary>Full account number: admins only (spec #31). Never log.</summary>
public record PayoutDto(string BankName, string AccountNumber, string AccountName);

// GET /admin/withdrawals (#31)
public record AdminWithdrawalRowDto(
    string Reference,
    string CampaignTitle,
    string ApplicantName,
    long Amount,
    string Status,
    DateTime RequestedAt,
    bool IsNew,
    PayoutDto Payout);

// POST /admin/withdrawals/:ref/actions (#32)
public record WithdrawalActionRequest(
    string? Action, string? Note, string? Reason, string? Message, string? TransferReference);

public record WithdrawalActionResponse(string Reference, string Status);
