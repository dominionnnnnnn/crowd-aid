using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.Entities;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrowdAid.Service.Seeding;

/// <summary>
/// Creates the schema and seeds only what has no create screen (spec §7, fresh start):
/// the admin account and outreach events. Fictional demo campaigns and donations are added
/// by DemoDataSeeder unless Seed__DemoData=false.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        // Postgres may still be starting in Docker: retry.
        // Swap EnsureCreated() for Migrate() once EF migrations exist.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(ct);
                break;
            }
            catch (Exception ex) when (attempt < 15)
            {
                logger.LogWarning("DB not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(2000, ct);
            }
        }

        await SeedAdminAsync(db, sp, logger, ct);
        await SeedOutreachAsync(db, ct);

        // Fictional demo campaigns and donations (on by default; set Seed__DemoData=false to skip).
        if (sp.GetRequiredService<IConfiguration>().GetValue("Seed:DemoData", true))
            await DemoDataSeeder.SeedAsync(db, logger, ct);
    }

    private static async Task SeedAdminAsync(AppDbContext db, IServiceProvider sp, ILogger logger, CancellationToken ct)
    {
        var admin = sp.GetRequiredService<IOptions<AdminSettings>>().Value;
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning("Admin__Email / Admin__Password not set: no admin account seeded.");
            return;
        }

        var email = admin.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return;

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new User
        {
            Name = admin.Name,
            Email = email,
            Role = UserRole.Admin,
            PasswordHash = hasher.Hash(admin.Password),
            ActivatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedOutreachAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Outreaches.AnyAsync(ct)) return;

        // Fictional demo event. Edit the details here; there is no create screen.
        db.Outreaches.Add(new Outreach
        {
            Id = "o1",
            Title = "Community fundraising outreach",
            Date = new DateOnly(2026, 11, 28),
            Time = "9:00am – 1:00pm",
            Venue = "Meeting point shared after you register",
        });
        await db.SaveChangesAsync(ct);
    }
}
