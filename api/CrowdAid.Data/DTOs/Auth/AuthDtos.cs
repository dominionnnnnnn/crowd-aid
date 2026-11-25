namespace CrowdAid.Data.DTOs.Auth;

// POST /admin/auth/login (#12) and /auth/login (#22)
public record LoginRequest(string? Email, string? Password);

public record AdminSummaryDto(Guid Id, string Name, string Role);

/// <summary>#12 response.</summary>
public record AdminLoginResponse(string Token, int ExpiresIn, AdminSummaryDto Admin);

/// <summary>#25 response, and the "user" part of #22 / #24.</summary>
public record UserSummaryDto(Guid Id, string Name, string Email, string Role);

/// <summary>#22 and #24 response.</summary>
public record UserLoginResponse(string Token, int ExpiresIn, UserSummaryDto User);

// POST /auth/activation/check (#23)
public record ActivationCheckRequest(string? Token);

public record ActivationCheckResponse(
    bool Valid, string Name, string EmailMasked, string? ApplicationRef, string? PatientName);

// POST /auth/activate (#24)
public record ActivateRequest(string? Token, string? Password);
