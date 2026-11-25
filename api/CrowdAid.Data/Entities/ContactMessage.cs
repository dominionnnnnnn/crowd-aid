namespace CrowdAid.Data.Entities;

public class ContactMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = "";
    public string? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
