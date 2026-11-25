using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.DTOs.Public;

namespace CrowdAid.Data.IRepo;

public interface ICampaignService
{
    Task<CampaignListResponse> ListAsync(string? q, string? location, string? need, bool? featured, CancellationToken ct = default);
    Task<CampaignDetailDto> GetPublishedAsync(string id, CancellationToken ct = default);
}

public interface IApplicationService
{
    Task<ApplicationCreatedResponse> SubmitAsync(CreateApplicationRequest request, CancellationToken ct = default);
    Task<ApplicationStatusResponse> GetStatusAsync(string? reference, string? email, CancellationToken ct = default);
}

public interface IDonationService
{
    Task<DonationStartedResponse> StartAsync(CreateDonationRequest request, CancellationToken ct = default);
    Task<DonationStatusResponse> GetStatusAsync(string reference, CancellationToken ct = default);
    /// <summary>Paystack webhook (spec #35). Throws UnauthenticatedException on a bad signature.</summary>
    Task HandleWebhookAsync(string rawBody, string? signature, CancellationToken ct = default);
}

/// <summary>Practical help, outreach, volunteers and contact messages.</summary>
public interface ICommunityService
{
    Task<OfferCreatedResponse> CreateOfferAsync(CreateOfferRequest request, CancellationToken ct = default);
    Task<ListResponse<OutreachDto>> ListOutreachAsync(CancellationToken ct = default);
    Task<OutreachVolunteerResponse> RegisterForOutreachAsync(OutreachVolunteerRequest request, CancellationToken ct = default);
    Task<ReferenceResponse> JoinCommunityAsync(CommunityVolunteerRequest request, CancellationToken ct = default);
    Task<ReferenceResponse> CreateContactMessageAsync(CreateContactMessageRequest request, CancellationToken ct = default);
}
