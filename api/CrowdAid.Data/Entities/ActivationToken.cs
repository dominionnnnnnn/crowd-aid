namespace CrowdAid.Data.Entities;

/// <summary>One-time activation link (spec approve step 4). Only the SHA-256 hash is stored.</summary>
public class ActivationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    /// <summary>Set when a newer link replaces this one (spec #33).</summary>
    public DateTime? RevokedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
