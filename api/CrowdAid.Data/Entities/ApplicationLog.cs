namespace CrowdAid.Data.Entities;

public class ApplicationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    /// <summary>e.g. "Opened", "Approved", "Activation link re-sent"</summary>
    public string Action { get; set; } = "";
    public string? Note { get; set; }
    /// <summary>Private reason (reject) or what's needed (request info).</summary>
    public string? Reason { get; set; }
    /// <summary>Message the admin sends by phone/WhatsApp. Logged only, never sent by the app.</summary>
    public string? Message { get; set; }
    /// <summary>Admin's display name.</summary>
    public string By { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}
