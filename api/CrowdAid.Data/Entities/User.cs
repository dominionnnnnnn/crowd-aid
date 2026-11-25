using CrowdAid.Data.Constants;

namespace CrowdAid.Data.Entities;

/// <summary>Admins and campaigners share one table (spec §6, correction 3).</summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    /// <summary>Unique, always stored lower-case.</summary>
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    /// <summary>BCrypt hash. Null until a campaigner activates their account.</summary>
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = UserRole.Campaigner;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
