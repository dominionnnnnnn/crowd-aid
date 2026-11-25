namespace CrowdAid.Data.DTOs.Public;

// All request properties are nullable on purpose: the service validates them and returns
// the spec's field messages instead of ASP.NET's default "The X field is required.".

// POST /applications (spec #3)
public record CreateApplicationRequest(
    string? Who,
    string? Name,
    string? Phone,
    string? Email,
    string? Relationship,
    string? PatientName,
    int? PatientAge,
    string? Town,
    string? Lga,
    string? Condition,
    string? Treatment,
    string? Facility,
    long? EstimatedCost,
    long? AmountNeeded,
    List<string>? SupportTypes,
    bool? PrivateReviewConsent,
    bool? AccuracyConfirmed);

public record ApplicationCreatedResponse(string Reference, string Status);

// GET /applications/status?reference=&email= (spec #34)
public record ApplicationStatusResponse(string Reference, string Status, string Message);
