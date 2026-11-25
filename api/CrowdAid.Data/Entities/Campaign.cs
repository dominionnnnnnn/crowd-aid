namespace CrowdAid.Data.Entities;

/// <summary>Public campaign created by Publish (spec #17).</summary>
public class Campaign
{
    /// <summary>Slug, e.g. "usman" (or "usman-2" if taken).</summary>
    public string Id { get; set; } = "";
    public string? ApplicationReference { get; set; }
    public Guid? OwnerUserId { get; set; }
    public User? Owner { get; set; }

    public string Title { get; set; } = "";
    public string Condition { get; set; } = "";
    public string Town { get; set; } = "";
    public string Location { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Preview { get; set; } = "";
    public List<string> Story { get; set; } = new();

    /// <summary>Confirmed donations only. Updated by the confirm rule (spec #6).</summary>
    public long Raised { get; set; }
    public long Goal { get; set; }
    public int Supporters { get; set; }

    public List<string> Needs { get; set; } = new();
    public List<FundItem> Funds { get; set; } = new();
    public string OrganizerName { get; set; } = "";
    public string OrganizerRole { get; set; } = "";

    public bool Reviewed { get; set; } = true;
    public bool Published { get; set; } = true;
    public bool Featured { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CampaignUpdate> Updates { get; set; } = new List<CampaignUpdate>();
}

/// <summary>"How the funds will be used" line. Stored as JSON on the campaign row.</summary>
public class FundItem
{
    public string Label { get; set; } = "";
    public long Amount { get; set; }
}
