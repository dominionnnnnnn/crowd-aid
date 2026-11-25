using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.DTOs.Auth;
using CrowdAid.Data.DTOs.Common;

namespace CrowdAid.Data.IRepo;

/// <summary>Sign-in, activation and session restore (spec #12, #22–25).</summary>
public interface IAuthService
{
    Task<AdminLoginResponse> AdminLoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserLoginResponse> CampaignerLoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ActivationCheckResponse> CheckActivationAsync(ActivationCheckRequest request, CancellationToken ct = default);
    Task<UserLoginResponse> ActivateAsync(ActivateRequest request, CancellationToken ct = default);
    Task<UserSummaryDto> GetMeAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Review, approve and publish (spec #13–17, #33).</summary>
public interface IAdminApplicationService
{
    Task<ListResponse<AdminApplicationRowDto>> ListAsync(string? status, CancellationToken ct = default);
    Task<AdminApplicationDetailDto> GetAsync(string reference, string adminName, CancellationToken ct = default);
    Task<AdminApplicationDetailDto> UpdateVerificationAsync(string reference, VerificationRequest request, string adminName, CancellationToken ct = default);
    Task<ApplicationActionResponse> ActionAsync(string reference, ApplicationActionRequest request, string adminName, Guid adminId, CancellationToken ct = default);
    Task<PublishResponse> PublishAsync(string reference, PublishRequest request, string adminName, CancellationToken ct = default);
    Task<AccountLinkDto> ResendActivationAsync(string reference, string adminName, Guid adminId, CancellationToken ct = default);
}

/// <summary>Offers, volunteers and messages tabs (spec #18–21).</summary>
public interface IAdminInboxService
{
    Task<ListResponse<AdminOfferRowDto>> ListOffersAsync(string? status, string? campaignId, CancellationToken ct = default);
    Task<AdminOfferRowDto> UpdateOfferStatusAsync(string reference, UpdateOfferRequest request, CancellationToken ct = default);
    Task<ListResponse<AdminVolunteerRowDto>> ListVolunteersAsync(string? outreachId, string? kind, CancellationToken ct = default);
    Task<ListResponse<AdminMessageRowDto>> ListMessagesAsync(CancellationToken ct = default);
}

/// <summary>Admin side of demo withdrawals (spec #31–32). No money moves.</summary>
public interface IAdminWithdrawalService
{
    Task<ListResponse<AdminWithdrawalRowDto>> ListAsync(string? status, CancellationToken ct = default);
    Task<WithdrawalActionResponse> ActionAsync(string reference, WithdrawalActionRequest request, string adminName, Guid adminId, CancellationToken ct = default);
}
