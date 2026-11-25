using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Auth;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>Admin and campaigner sign-in, activation links, session restore (spec #12, #22–25).</summary>
public class AuthService(
    AppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokenService,
    IActivationTokenFactory activationTokens) : IAuthService
{
    private const string BadCredentials = "Email or password is incorrect.";
    private static string? _dummyHash;

    public async Task<AdminLoginResponse> AdminLoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await SignInAsync(request, UserRole.Admin, ct);
        var (token, expiresIn) = tokenService.CreateAccessToken(user);
        return new AdminLoginResponse(token, expiresIn, new AdminSummaryDto(user.Id, user.Name, user.Role));
    }

    public async Task<UserLoginResponse> CampaignerLoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await SignInAsync(request, UserRole.Campaigner, ct);
        return CreateLoginResponse(user);
    }

    public async Task<ActivationCheckResponse> CheckActivationAsync(ActivationCheckRequest request, CancellationToken ct = default)
    {
        var token = await FindUsableTokenAsync(request.Token, ct);

        // The most recent approved application for this user (spec: "latest Approved/Published").
        var application = await db.Applications.AsNoTracking()
            .Where(a => a.UserId == token.UserId &&
                        (a.Status == ApplicationStatus.Approved || a.Status == ApplicationStatus.Published))
            .OrderByDescending(a => a.ReceivedAt)
            .Select(a => new { a.Reference, a.PatientName })
            .FirstOrDefaultAsync(ct);

        return new ActivationCheckResponse(true, token.User.Name, MaskEmail(token.User.Email),
            application?.Reference, application?.PatientName);
    }

    public async Task<UserLoginResponse> ActivateAsync(ActivateRequest request, CancellationToken ct = default)
    {
        var token = await FindUsableTokenAsync(request.Token, ct);

        var v = new FormValidator();
        var password = request.Password ?? "";
        if (password.Length < Lookups.MinimumPasswordLength)
            v.Add("password", $"Use at least {Lookups.MinimumPasswordLength} characters.");
        else if (password.Length > 128)
            v.Add("password", "Use fewer than 128 characters.");
        v.ThrowIfInvalid();

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // One winner if the link is used twice at once: only a live, unused token can be claimed.
        var claimed = await db.ActivationTokens
            .Where(t => t.Id == token.Id && t.UsedAt == null && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);
        if (claimed == 0)
            throw new GoneException("TOKEN_USED", "This link was already used. Sign in instead.");

        var user = await db.Users.FirstAsync(u => u.Id == token.UserId, ct);
        user.PasswordHash = hasher.Hash(password);
        user.ActivatedAt ??= now;
        user.LastLoginAt = now;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return CreateLoginResponse(user);
    }

    public async Task<UserSummaryDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthenticatedException();
        return new UserSummaryDto(user.Id, user.Name, user.Email, user.Role);
    }

    // ---- helpers ----

    /// <summary>
    /// Same generic 401 for an unknown email, a wrong password, a wrong role, and a campaigner
    /// who hasn't activated yet, so nothing reveals which accounts exist.
    /// </summary>
    private async Task<User> SignInAsync(LoginRequest request, string role, CancellationToken ct)
    {
        var v = new FormValidator();
        var email = v.Email("email", request.Email);
        if (string.IsNullOrEmpty(request.Password)) v.Add("password", "Enter your password.");
        v.ThrowIfInvalid();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var hash = user?.PasswordHash;
        var matches = hasher.Verify(request.Password!, hash ?? (_dummyHash ??= hasher.Hash(Guid.NewGuid().ToString())));

        if (user is null || hash is null || !matches || user.Role != role)
            throw new UnauthenticatedException("INVALID_CREDENTIALS", BadCredentials);

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return user;
    }

    private UserLoginResponse CreateLoginResponse(User user)
    {
        var (token, expiresIn) = tokenService.CreateAccessToken(user);
        return new UserLoginResponse(token, expiresIn, new UserSummaryDto(user.Id, user.Name, user.Email, user.Role));
    }

    /// <summary>404 unknown, 410 TOKEN_USED / TOKEN_EXPIRED. Does not use the token up.</summary>
    private async Task<ActivationToken> FindUsableTokenAsync(string? raw, CancellationToken ct)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
            throw new NotFoundException("This link isn't valid. Ask CrowdAid to send a new one.");

        var hash = activationTokens.Hash(value);
        var token = await db.ActivationTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new NotFoundException("This link isn't valid. Ask CrowdAid to send a new one.");

        if (token.UsedAt != null)
            throw new GoneException("TOKEN_USED", "This link was already used. Sign in instead.");
        // A link replaced by a newer one (spec #33) counts as expired.
        if (token.RevokedAt != null || token.ExpiresAt <= DateTime.UtcNow)
            throw new GoneException("TOKEN_EXPIRED", "This link has expired. Ask CrowdAid to send a new one.");

        return token;
    }

    /// <summary>"aminat@example.com" -> "a•••@example.com"</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 0 ? "•••" : $"{email[0]}•••{email[at..]}";
    }
}
