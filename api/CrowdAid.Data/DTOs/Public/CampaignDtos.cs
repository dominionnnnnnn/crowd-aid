namespace CrowdAid.Data.DTOs.Public;

// GET /campaigns (spec #1)
public record CampaignCardDto(
    string Id,
    string Title,
    string Condition,
    string Town,
    string Location,
    string? ImageUrl,
    string Preview,
    long Raised,
    long Goal,
    int Supporters,
    IReadOnlyList<string> Needs,
    bool Reviewed);

public record CampaignListResponse(IReadOnlyList<CampaignCardDto> Data, IReadOnlyList<string> Locations);

// GET /campaigns/:id (spec #2)
public record CampaignDetailDto(
    string Id,
    string Title,
    string Condition,
    string Town,
    string Location,
    bool Reviewed,
    long Raised,
    long Goal,
    int Supporters,
    IReadOnlyList<string> Story,
    IReadOnlyList<string> Needs,
    IReadOnlyList<FundItemDto> Funds,
    IReadOnlyList<CampaignUpdateDto> Updates,
    OrganizerDto Organizer);

public record FundItemDto(string Label, long Amount);

public record CampaignUpdateDto(DateOnly Date, string Title, string Text);

public record OrganizerDto(string Name, string Role);

public record CampaignTotalsDto(long Raised, long Goal, int Supporters);
