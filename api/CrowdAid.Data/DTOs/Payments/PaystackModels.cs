namespace CrowdAid.Data.DTOs.Payments;

/// <summary>What CrowdAid asks Paystack to start (spec #5). Amount is in whole naira here; the client converts to kobo.</summary>
public record PaystackInitializeRequest(
    string Email,
    long AmountNaira,
    string Reference,
    string CallbackUrl,
    string Channel,
    string CampaignId);

public record PaystackInitializeResult(string AuthorizationUrl, string AccessCode, string Reference);

/// <summary>The fields CrowdAid needs from Paystack's verify response / charge.success event.</summary>
public record PaystackTransaction(
    string Reference,
    string Status,
    long AmountKobo,
    string Currency,
    DateTime? PaidAt);
