using System.Net.Mail;
using Microsoft.EntityFrameworkCore;

// CrowdAid API scaffold. Proves the wiring: API <-> Postgres, API -> MailHog (SMTP).
// The backend dev replaces/extends this with the real endpoints from api-spec-v3.md.

var builder = WebApplication.CreateBuilder(args);

// ---- Database (Postgres via EF Core) ----
var connString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings__Default is not set");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connString));

// ---- CORS: only needed in dev (in prod Caddy serves site + /api on one origin) ----
var allowedOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:5173")
    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();

// ---- Create tables + seed on startup (retry until Postgres is ready) ----
// Swap EnsureCreated() for Migrate() once EF migrations exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            if (!db.Greetings.Any())
            {
                db.Greetings.Add(new Greeting { Message = "Hello from CrowdAid + Postgres 👋" });
                db.SaveChanges();
            }
            break;
        }
        catch (Exception ex) when (attempt < 15)
        {
            app.Logger.LogWarning("DB not ready (attempt {Attempt}): {Msg}", attempt, ex.Message);
            Thread.Sleep(2000);
        }
    }
}

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

api.MapGet("/hello", async (AppDbContext db) =>
{
    var greeting = await db.Greetings.OrderBy(g => g.Id).FirstAsync();
    return Results.Ok(new { greeting.Id, greeting.Message, servedAt = DateTime.UtcNow });
});

// ---- Email test: sends one message to MailHog ----
// Only works while SMTP points at MailHog, so it can never send real mail.
// DELETE this endpoint before switching to a real email provider.
api.MapPost("/test-email", async (TestEmailDto dto, IConfiguration cfg) =>
{
    var host = cfg["Smtp:Host"] ?? "";
    if (!host.Equals("mailhog", StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();

    MailAddress to;
    try { to = new MailAddress(dto.To ?? ""); }
    catch { return Results.BadRequest(new { error = new { code = "VALIDATION_ERROR", fields = new { to = "Enter a valid email." } } }); }

    var baseUrl = (cfg["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
    var link = $"{baseUrl}/activate?token=TEST-ONLY";

    using var msg = new MailMessage
    {
        From = new MailAddress(cfg["Smtp:From"] ?? "CrowdAid <no-reply@crowdaid.demo>"),
        Subject = "CrowdAid: set up your account (test)",
        IsBodyHtml = true,
        Body = $"<p>Hello,</p><p>Your CrowdAid application was approved. Set your password here:</p>" +
               $"<p><a href=\"{link}\">{link}</a></p><p>This link works once and expires in 72 hours.</p>" +
               "<p><em>Test email sent by the CrowdAid scaffold.</em></p>",
    };
    msg.To.Add(to);

    using var smtp = new SmtpClient(host, int.TryParse(cfg["Smtp:Port"], out var p) ? p : 1025) { EnableSsl = false };
    await smtp.SendMailAsync(msg);
    return Results.Ok(new { sent = true, to = to.Address });
});

app.Run();

// ---- Models ----
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Greeting> Greetings => Set<Greeting>();
}

public class Greeting
{
    public int Id { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record TestEmailDto(string? To);
