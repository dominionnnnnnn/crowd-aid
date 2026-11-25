using CrowdAid.Data.Constants;
using CrowdAid.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ActivationToken> ActivationTokens => Set<ActivationToken>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();
    public DbSet<ApplicationFile> ApplicationFiles => Set<ApplicationFile>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignUpdate> CampaignUpdates => Set<CampaignUpdate>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Outreach> Outreaches => Set<Outreach>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<PayoutDetails> PayoutDetails => Set<PayoutDetails>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<WithdrawalLog> WithdrawalLogs => Set<WithdrawalLog>();

    /// <summary>Postgres sequence used for each reference prefix (CA-REQ-0001 …).</summary>
    public static readonly IReadOnlyDictionary<string, string> ReferenceSequences = new Dictionary<string, string>
    {
        [ReferencePrefix.Application] = "seq_ref_application",
        [ReferencePrefix.Donation] = "seq_ref_donation",
        [ReferencePrefix.Offer] = "seq_ref_offer",
        [ReferencePrefix.Volunteer] = "seq_ref_volunteer",
        [ReferencePrefix.Message] = "seq_ref_message",
        [ReferencePrefix.Withdrawal] = "seq_ref_withdrawal",
    };

    protected override void OnModelCreating(ModelBuilder b)
    {
        foreach (var seq in ReferenceSequences.Values)
            b.HasSequence<long>(seq).StartsAt(1).IncrementsBy(1);

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Role).HasMaxLength(20);
        });

        b.Entity<ActivationToken>(e =>
        {
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<Application>(e =>
        {
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.User).WithMany(u => u.Applications).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Campaign).WithMany().HasForeignKey(x => x.CampaignId);
            e.OwnsOne(x => x.Publication, p => p.ToJson());
        });

        b.Entity<ApplicationLog>(e =>
            e.HasOne(x => x.Application).WithMany(a => a.Log).HasForeignKey(x => x.ApplicationId));

        b.Entity<ApplicationFile>(e =>
            e.HasOne(x => x.Application).WithMany(a => a.Files).HasForeignKey(x => x.ApplicationId));

        b.Entity<Campaign>(e =>
        {
            e.Property(x => x.Id).HasMaxLength(80);
            e.HasIndex(x => x.Published);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId);
            e.OwnsMany(x => x.Funds, f => f.ToJson());
        });

        b.Entity<CampaignUpdate>(e =>
            e.HasOne(x => x.Campaign).WithMany(c => c.Updates).HasForeignKey(x => x.CampaignId));

        b.Entity<Donation>(e =>
        {
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => new { x.CampaignId, x.Status });
        });

        b.Entity<Offer>(e => e.HasIndex(x => x.Reference).IsUnique());
        b.Entity<Volunteer>(e => e.HasIndex(x => x.Reference).IsUnique());
        b.Entity<ContactMessage>(e => e.HasIndex(x => x.Reference).IsUnique());

        b.Entity<PayoutDetails>(e =>
        {
            e.HasKey(x => x.UserId);
            e.HasOne(x => x.User).WithOne().HasForeignKey<PayoutDetails>(x => x.UserId);
        });

        b.Entity<Withdrawal>(e =>
        {
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => new { x.CampaignId, x.Status });
        });

        b.Entity<WithdrawalLog>(e =>
            e.HasOne(x => x.Withdrawal).WithMany(w => w.Log).HasForeignKey(x => x.WithdrawalId));
    }
}
