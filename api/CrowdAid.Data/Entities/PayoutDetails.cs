namespace CrowdAid.Data.Entities;

/// <summary>P1. Sensitive: never log. Campaigner sees the number masked (spec #29).</summary>
public class PayoutDetails
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
