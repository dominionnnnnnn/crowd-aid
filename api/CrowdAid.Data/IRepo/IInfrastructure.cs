using CrowdAid.Data.DTOs.Payments;
using CrowdAid.Data.Entities;

namespace CrowdAid.Data.IRepo;

// Abstractions the services depend on. Implementations live in CrowdAid.Service/Infrastructure.
// Add your feature interfaces (ICampaignService, IApplicationService, IDonationService …) in this folder too.

public interface IEmailSender
{
    /// <summary>Sends through SMTP (MailHog in the demo). Returns false instead of throwing on failure.</summary>
    Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface IPaystackClient
{
    Task<PaystackInitializeResult> InitializeAsync(PaystackInitializeRequest request, CancellationToken ct = default);
    Task<PaystackTransaction> VerifyAsync(string reference, CancellationToken ct = default);
    /// <summary>x-paystack-signature: hex HMAC-SHA512 of the raw body with the secret key.</summary>
    bool IsValidSignature(string rawBody, string? signatureHeader);
}

public interface ITokenService
{
    /// <summary>Signed JWT with sub, name, role (spec §5a). Returns the token and its lifetime in seconds.</summary>
    (string Token, int ExpiresIn) CreateAccessToken(User user);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IReferenceGenerator
{
    /// <summary>Next reference for a prefix from its Postgres sequence, e.g. "CA-REQ-0007".</summary>
    Task<string> NextAsync(string prefix, CancellationToken ct = default);
}

public interface IActivationTokenFactory
{
    /// <summary>32 random bytes, URL-safe. Store only <see cref="Hash"/>.</summary>
    string CreateRawToken();
    string Hash(string rawToken);
}
