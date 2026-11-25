namespace CrowdAid.Data.Entities;

/// <summary>Seeded (no create screen). Spec #8.</summary>
public class Outreach
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public DateOnly Date { get; set; }
    /// <summary>Display text, e.g. "9:00am – 1:00pm".</summary>
    public string Time { get; set; } = "";
    public string Venue { get; set; } = "";
    public string? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
}
