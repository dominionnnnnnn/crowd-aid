using CrowdAid.Data.Constants;

namespace CrowdAid.Data.Entities;

public class Donation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>CA-DON-0001. Also sent to Paystack as the transaction reference.</summary>
    public string Reference { get; set; } = "";
    public string CampaignId { get; set; } = "";
    public Campaign Campaign { get; set; } = null!;

    /// <summary>Whole naira.</summary>
    public long Amount { get; set; }
    public long PlatformFee { get; set; }
    public long Total { get; set; }

    public string? Name { get; set; }
    public bool Anonymous { get; set; }
    /// <summary>Private. Never returned by public or campaigner endpoints.</summary>
    public string Email { get; set; } = "";
    public string Method { get; set; } = PaymentMethod.Card;

    public string Status { get; set; } = DonationStatus.Pending;
    /// <summary>Paystack's raw status, e.g. "abandoned".</summary>
    public string? GatewayStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool TestMode { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
