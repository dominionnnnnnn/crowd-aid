namespace CrowdAid.Api.Common;

public static class ApiRoutes
{
    /// <summary>
    /// Prefix for every spec endpoint: GET /campaigns is served at /api/v1/campaigns.
    /// Caddy forwards /api/* to this API. Agreed with the frontend: change it here only.
    /// </summary>
    public const string Prefix = "api/v1";

    public const string Admin = Prefix + "/admin";
    public const string Me = Prefix + "/me";
}

public static class Policies
{
    public const string Admin = "admin";
    public const string Campaigner = "campaigner";
    public const string AnySignedIn = "any";
}

public static class RateLimits
{
    /// <summary>Logins and activation: 5 per minute per IP (spec §5a).</summary>
    public const string Auth = "auth";
    /// <summary>Public form posts (spec §10.5).</summary>
    public const string PublicForms = "public-forms";
}
