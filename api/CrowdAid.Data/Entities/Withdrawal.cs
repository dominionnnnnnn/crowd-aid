using CrowdAid.Data.Constants;

namespace CrowdAid.Data.Entities;

/// <summary>P1, simulated (spec #30–#32).</summary>
public class Withdrawal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = "";
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string CampaignId { get; set; } = "";
    public Campaign Campaign { get; set; } = null!;
    public long Amount { get; set; }
    public string Status { get; set; } = WithdrawalStatus.Requested;
    /// <summary>Message shown to the campaigner (on reject).</summary>
    public string? Note { get; set; }
    /// <summary>Private admin reason.</summary>
    public string? Reason { get; set; }
    public string? TransferReference { get; set; }
    public bool Demo { get; set; } = true;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public Guid? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public ICollection<WithdrawalLog> Log { get; set; } = new List<WithdrawalLog>();
}

public class WithdrawalLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WithdrawalId { get; set; }
    public Withdrawal Withdrawal { get; set; } = null!;
    public string Action { get; set; } = "";
    public string By { get; set; } = "";
    public string? Note { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
