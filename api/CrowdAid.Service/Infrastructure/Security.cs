using System.Security.Cryptography;
using System.Text;
using CrowdAid.Data.IRepo;

namespace CrowdAid.Service.Infrastructure;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11; // spec: cost 10–12

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class ActivationTokenFactory : IActivationTokenFactory
{
    public string CreateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // URL-safe base64, no padding
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
