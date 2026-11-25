using System.Text;
using System.Threading.RateLimiting;
using CrowdAid.Api.Common;
using CrowdAid.Api.Middleware;
using CrowdAid.Data;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.Options;
using CrowdAid.Service;
using CrowdAid.Service.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// CrowdAid API — onion layout:
//   CrowdAid.Api (this)  ->  CrowdAid.Service  ->  CrowdAid.Data
// Contract: ../api-spec-v3.md. Config comes from environment variables (compose files).

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---- Database (Postgres via EF Core) ----
var connString = config.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings__Default is not set");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connString));

// ---- Services and infrastructure (CrowdAid.Service) ----
builder.Services.AddCrowdAidServices(config);

// ---- Controllers + the spec's error shape for model-binding errors ----
builder.Services.AddControllers();

// ---- Swagger (Development only, see below) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "CrowdAid API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the token from the login response (no 'Bearer ' prefix).",
    });
    o.AddSecurityRequirement(new()
    {
        [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
    });
});
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var fields = new Dictionary<string, string>();
        foreach (var (key, entry) in ctx.ModelState)
        {
            if (entry.Errors.Count == 0) continue;
            var message = entry.Errors[0].ErrorMessage;
            fields.TryAdd(FieldName(key), string.IsNullOrEmpty(message) ? "This value isn't valid." : message);
        }
        return new BadRequestObjectResult(ErrorResponse.Of("VALIDATION_ERROR", "Please fix the highlighted fields.", fields));
    };
});
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

// ---- Auth: JWT bearer, roles admin / campaigner (spec §5a) ----
var jwt = config.GetSection(JwtSettings.Section).Get<JwtSettings>() ?? new JwtSettings();
if (Encoding.UTF8.GetByteCount(jwt.Secret) < 32)
    throw new InvalidOperationException("Jwt__Secret must be at least 32 characters");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // keep "sub", "name", "role" as-is
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
        o.Events = new JwtBearerEvents
        {
            // 401 in the spec's error shape; TOKEN_EXPIRED lets the UI say "Your session expired".
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                var expired = ctx.AuthenticateFailure is SecurityTokenExpiredException;
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(expired
                    ? ErrorResponse.Of("TOKEN_EXPIRED", "Your session expired. Sign in again.")
                    : ErrorResponse.Of("UNAUTHENTICATED", "Please sign in."));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(ErrorResponse.Of("FORBIDDEN", "You can't do this."));
            },
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.Admin, p => p.RequireAuthenticatedUser().RequireRole("admin"));
    o.AddPolicy(Policies.Campaigner, p => p.RequireAuthenticatedUser().RequireRole("campaigner"));
    o.AddPolicy(Policies.AnySignedIn, p => p.RequireAuthenticatedUser());
});

// ---- Rate limits (per client IP) ----
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.OnRejected = async (ctx, ct) =>
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            ErrorResponse.Of("TOO_MANY_REQUESTS", "Too many attempts. Please wait a minute and try again."), ct);

    o.AddPolicy(RateLimits.Auth, http => RateLimitPartition.GetFixedWindowLimiter(
        ClientIp(http), _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy(RateLimits.PublicForms, http => RateLimitPartition.GetFixedWindowLimiter(
        ClientIp(http), _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

// ---- CORS: only needed in dev (in prod Caddy serves site + /api on one origin) ----
var allowedOrigins = (config["CORS_ORIGINS"] ?? "http://localhost:5173")
    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// No UseHttpsRedirection(): Caddy terminates HTTPS in front of this API.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "CrowdAid API v1"));
}
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DbInitializer.InitializeAsync(app.Services);

app.Run();

// "$.amount" / "Amount" -> "amount", to match the request JSON field names.
static string FieldName(string key)
{
    var k = key.TrimStart('$', '.');
    return k.Length == 0 ? "body" : char.ToLowerInvariant(k[0]) + k[1..];
}

// Caddy sets X-Forwarded-For; fall back to the socket address in dev.
static string ClientIp(HttpContext http)
    => http.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
       ?? http.Connection.RemoteIpAddress?.ToString()
       ?? "unknown";
