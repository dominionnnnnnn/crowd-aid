namespace CrowdAid.Data.Entities;

public class CampaignUpdate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CampaignId { get; set; } = "";
    public Campaign Campaign { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}
