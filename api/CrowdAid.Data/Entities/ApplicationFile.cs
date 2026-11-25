namespace CrowdAid.Data.Entities;

/// <summary>P1: optional private evidence uploads (spec #4).</summary>
public class ApplicationFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public string FileName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
