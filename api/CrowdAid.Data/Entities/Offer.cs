using CrowdAid.Data.Constants;

namespace CrowdAid.Data.Entities;

/// <summary>Practical help offer (spec #7). Never public.</summary>
public class Offer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = "";
    /// <summary>Null = general mode.</summary>
    public string? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public List<string> Types { get; set; } = new();
    public string? Details { get; set; }
    public List<string> Availability { get; set; } = new();
    public string Area { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool ContactConsent { get; set; }
    public string Status { get; set; } = OfferStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
