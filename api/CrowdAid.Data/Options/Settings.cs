namespace CrowdAid.Data.Options;

// Bound from configuration. Environment variables use double underscores,
// e.g. Jwt__Secret, Smtp__Host, Paystack__SecretKey (see docker-compose files).

public class AppSettings
{
    public const string Section = "App";
    /// <summary>Public site URL, used in activation links and the Paystack callback.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5173";
}

public class JwtSettings
{
    public const string Section = "Jwt";
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "crowdaid-api";
    public string Audience { get; set; } = "crowdaid-api";
    public int ExpiryMinutes { get; set; } = 480;
}

public class SmtpSettings
{
    public const string Section = "Smtp";
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "mailhog";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "CrowdAid <no-reply@crowdaid.demo>";
}

public class PaystackSettings
{
    public const string Section = "Paystack";
    /// <summary>sk_test_… Server only. Never send to the frontend or log.</summary>
    public string SecretKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.paystack.co";
    /// <summary>Defaults to {App:BaseUrl}/donate/result when empty.</summary>
    public string? CallbackUrl { get; set; }
}

public class AdminSettings
{
    public const string Section = "Admin";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Name { get; set; } = "CrowdAid Admin";
}
