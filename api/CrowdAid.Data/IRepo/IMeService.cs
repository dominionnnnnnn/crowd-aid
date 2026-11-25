using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.DTOs.Me;

namespace CrowdAid.Data.IRepo;

/// <summary>
/// Campaigner dashboard (spec #26–30). Every method takes the signed-in user's id from the token and
/// finds that user's own application and campaign. Ids from the URL or body are never used.
/// </summary>
public interface IMeService
{
    Task<MeCampaignResponse> GetCampaignAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResponse<MeDonationDto>> ListDonationsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<ListResponse<MeOfferDto>> ListOffersAsync(Guid userId, string? status, CancellationToken ct = default);
    Task<MaskedPayoutDto?> GetPayoutAsync(Guid userId, CancellationToken ct = default);
    Task<MaskedPayoutDto> SavePayoutAsync(Guid userId, PayoutRequest request, CancellationToken ct = default);
    Task<MeWithdrawalsResponse> ListWithdrawalsAsync(Guid userId, CancellationToken ct = default);
    Task<WithdrawalCreatedResponse> RequestWithdrawalAsync(Guid userId, CreateWithdrawalRequest request, CancellationToken ct = default);
}
