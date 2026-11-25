using CrowdAid.Data.Constants;

namespace CrowdAid.Data.Entities;

/// <summary>Outreach registrations and community sign-ups share one table (spec #9, #10).</summary>
public class Volunteer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = "";
    public string Kind { get; set; } = VolunteerKind.Outreach;
    public string? OutreachId { get; set; }
    public Outreach? Outreach { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string Area { get; set; } = "";
    /// <summary>Outreach roles, or community interests.</summary>
    public List<string> Roles { get; set; } = new();
    /// <summary>"all" | "part" for outreach. Null for community.</summary>
    public string? Stay { get; set; }
    /// <summary>Community only.</summary>
    public List<string> Availability { get; set; } = new();
    public bool Consent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
