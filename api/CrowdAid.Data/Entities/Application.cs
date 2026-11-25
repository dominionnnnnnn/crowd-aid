using CrowdAid.Data.Constants;

namespace CrowdAid.Data.Entities;

/// <summary>Healthcare assistance application (spec #3). Private: never returned by public endpoints.</summary>
public class Application
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>CA-REQ-0001</summary>
    public string Reference { get; set; } = "";

    // Section 1: applicant and patient
    public string Who { get; set; } = ApplicantWho.Myself;
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    /// <summary>Applicant's own email, lower-case. Sign-in and activation link.</summary>
    public string Email { get; set; } = "";
    public string Relationship { get; set; } = ApplicantWho.MyselfRelationship;
    public string PatientName { get; set; } = "";
    public int PatientAge { get; set; }
    public string Town { get; set; } = "";
    public string Lga { get; set; } = "";

    // Section 2: healthcare information
    public string Condition { get; set; } = "";
    public string Treatment { get; set; } = "";
    public string? Facility { get; set; }

    // Section 3: financial and practical assistance
    public long? EstimatedCost { get; set; }
    public long? AmountNeeded { get; set; }
    public List<string> SupportTypes { get; set; } = new();
    public bool PrivateReviewConsent { get; set; }
    public bool AccuracyConfirmed { get; set; }

    // Review
    public string Status { get; set; } = ApplicationStatus.Received;
    public string VerificationStatus { get; set; } = Constants.VerificationStatus.NotStarted;
    public string? VerificationMethod { get; set; }
    /// <summary>Public fields written by the admin at publish time (spec #17).</summary>
    public Publication? Publication { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationLog> Log { get; set; } = new List<ApplicationLog>();
    public ICollection<ApplicationFile> Files { get; set; } = new List<ApplicationFile>();
}

/// <summary>Stored as JSON on the application row.</summary>
public class Publication
{
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public long Goal { get; set; }
    public string? Organizer { get; set; }
    public string Story { get; set; } = "";
    public string ConsentRecorded { get; set; } = "";
    public bool PublicationConsent { get; set; }
    public bool ContentReviewed { get; set; }
}
