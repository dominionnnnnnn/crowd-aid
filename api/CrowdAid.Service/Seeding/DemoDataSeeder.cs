using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrowdAid.Service.Seeding;

/// <summary>
/// FICTIONAL demo data for the hackathon: published campaigns with confirmed donations,
/// so the home page, campaign list, campaign details and recent supporters have something
/// to show. No real patient, hospital, donor or payment is represented.
///
/// Rules this seeder keeps:
///  - Campaign.Raised equals the sum of its confirmed donations, and Supporters equals their count
///    (the same invariant the confirm rule keeps for real donations).
///  - Every donation is TestMode = true with a "CA-DON-DEMO-####" reference, so demo rows are
///    easy to spot and never collide with the real CA-DON-#### sequence.
///  - Runs once: skipped when any demo donation already exists.
///  - Turn off with config Seed__DemoData=false (on by default).
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>Last paragraph of every demo story. Set to null to leave it out.</summary>
    private const string? DemoNote =
        "Demo campaign: a fictional story created to demonstrate CrowdAid. No real patient, donation or payment.";

    private sealed record Update(int DaysAgo, string Title, string Text);

    private sealed record Plan(
        string Id, string Title, string Condition, string Town, string Location, string Preview,
        string[] Story, long Goal, long Raised, string[] Needs, (string Label, long Amount)[] Funds,
        string OrganizerName, string OrganizerRole, int PublishedDaysAgo, bool Featured, Update[] Updates);

    private static readonly Plan[] Plans =
    [
        new("usman",
            "Help Usman get his kidney surgery",
            "Kidney failure", "Abeokuta", "Abeokuta South, Ogun State",
            "Usman is 9 years old and loves football. His kidneys are failing, and surgery cannot wait much longer.",
            [
                "Usman is a 9-year-old boy in Abeokuta. Until a few months ago he was the fastest runner in his street and never missed a football game after school.",
                "Tests showed that his kidneys are failing. His doctors say he needs surgery, followed by several weeks of careful recovery and regular check-ups.",
                "His father drives a keke and his mother sells provisions. They have used their savings on tests and medicine, and the surgery cost is far beyond what they can raise alone.",
                "Every contribution brings Usman closer to the operating table. If you cannot give money, a ride to the hospital or a meal for the family also helps.",
            ],
            5_000_000, 3_000_000,
            ["Transport to hospital", "Blood donors", "Meals for family"],
            [("Kidney surgery", 3_500_000), ("Hospital stay and recovery", 900_000), ("Medication and tests", 600_000)],
            "Demo Family Representative", "Uncle of Usman", 24, true,
            [
                new(14, "Tests completed", "Usman finished the final round of tests this week. The doctors are now waiting for the funding target to schedule surgery."),
                new(6, "Thank you, Abeokuta", "The family says thank you to everyone who has given or shared this story. Usman asked how many people are cheering for him."),
            ]),

        new("funmilayo",
            "Support Funmilayo's cancer treatment",
            "Breast cancer", "Abeokuta", "Abeokuta North, Ogun State",
            "Funmilayo is a mother of three and a primary school teacher. She needs chemotherapy to start on time.",
            [
                "Funmilayo has taught Primary 3 pupils in Abeokuta for twelve years. She is a mother of three and the person her family leans on.",
                "She was diagnosed with breast cancer and her doctors have recommended chemotherapy. Each cycle needs medicine, tests and transport to the hospital.",
                "Her family has stretched their income to cover the first tests. They are asking the community to help her begin treatment without delay.",
            ],
            3_500_000, 1_850_000,
            ["Transport to hospital", "Professional support"],
            [("Chemotherapy cycles", 2_400_000), ("Scans and lab tests", 700_000), ("Medication and transport", 400_000)],
            "Demo Family Representative", "Husband of Funmilayo", 18, false,
            [
                new(9, "First cycle scheduled", "The first chemotherapy cycle has been scheduled. The family is arranging transport for each hospital visit."),
            ]),

        new("ibrahim",
            "Ibrahim needs surgery after a road accident",
            "Leg fracture after accident", "Abeokuta", "Abeokuta North, Ogun State",
            "A road accident left Ibrahim, a 27-year-old welder, unable to walk. Surgery will let him work again.",
            [
                "Ibrahim is a 27-year-old welder who supports his mother and two younger sisters. He was hurt in a road accident on his way to a job.",
                "His leg needs surgery and a metal implant. Until then he cannot stand or work, and the family has no income.",
                "A small amount has already been raised by his neighbours. The rest is needed for the operation and physiotherapy.",
            ],
            1_800_000, 1_260_000,
            ["Transport to hospital", "Medical equipment"],
            [("Surgery and implant", 1_200_000), ("Physiotherapy", 350_000), ("Crutches and medication", 250_000)],
            "Demo Community Member", "Neighbour of Ibrahim", 16, false,
            [
                new(10, "Crutches needed", "Ibrahim can start using crutches after surgery. The family is looking for a pair to borrow or buy."),
                new(3, "Over two-thirds raised", "Thank you for helping Ibrahim pass the two-thirds mark. The surgery date will be set once the target is close."),
            ]),

        new("baby-tobi",
            "Baby Tobi needs heart surgery",
            "Congenital heart condition", "Abeokuta", "Odeda, Ogun State",
            "Tobi is 8 months old. His doctors say a heart operation will help him grow strong like other children.",
            [
                "Tobi is 8 months old and the first child of his parents. He tires quickly when feeding and has been in and out of the clinic since he was born.",
                "His doctors have advised heart surgery in the next few months. The family is a farming household and cannot afford the full cost.",
                "The family is asking for the help of the community, and for anyone who can offer blood donation or transport on the day of surgery.",
            ],
            6_000_000, 780_000,
            ["Blood donors", "Transport to hospital", "Professional support"],
            [("Heart surgery", 4_800_000), ("Intensive care", 800_000), ("Medication and follow-up", 400_000)],
            "Demo Community Leader", "Community leader", 9, false,
            [
                new(4, "Early support received", "The first donations have come in. The family is gathering the medical papers the doctors asked for."),
            ]),

        new("mama-kemi",
            "Mama Kemi's eye surgery",
            "Cataract", "Abeokuta", "Abeokuta South, Ogun State",
            "Mama Kemi has sold akara at the same corner for thirty years. Cataracts are taking her sight.",
            [
                "Mama Kemi has sold akara at the same corner in Abeokuta for thirty years. Many people in the neighbourhood know her by name.",
                "Cataracts have clouded both eyes, and she can no longer work safely. Cataract surgery can restore much of her sight.",
                "She is close to her target. A little more will cover both eyes and her check-ups.",
            ],
            450_000, 420_000,
            ["Transport to hospital"],
            [("Surgery for both eyes", 340_000), ("Eye drops and glasses", 70_000), ("Check-ups", 40_000)],
            "Demo Family Representative", "Daughter of Mama Kemi", 21, false,
            [
                new(5, "Almost there", "Mama Kemi's family is almost at the target. They need a driver for the hospital appointment."),
            ]),

        new("sade",
            "Care for Sade through sickle cell crises",
            "Sickle cell disease", "Abeokuta", "Obafemi Owode, Ogun State",
            "Sade is 15 and wants to be a nurse. Sickle cell crises keep sending her to hospital.",
            [
                "Sade is 15 and in JSS3. She wants to become a nurse because of the nurses who have cared for her.",
                "She lives with sickle cell disease. Painful crises mean frequent hospital stays, medicines and blood tests.",
                "The family is raising a fund to cover her hospital care and medicines for the coming months.",
            ],
            900_000, 215_000,
            ["Blood donors", "Meals for family"],
            [("Hospital care during crises", 500_000), ("Daily medication", 250_000), ("Tests and transport", 150_000)],
            "Demo Family Representative", "Aunt of Sade", 6, false,
            [
                new(2, "Back in school", "Sade is out of hospital and back at school this week. Thank you to everyone who has supported her."),
            ]),
    ];

    private static readonly string[] FirstNames =
    [
        "Tunde", "Chioma", "Kunle", "Aisha", "Emeka", "Bisi", "Yusuf", "Ngozi", "Femi", "Zainab",
        "Segun", "Halima", "Ifeanyi", "Damilola", "Musa", "Titi", "Ayo", "Blessing", "Sola", "Rukayat",
    ];

    private static readonly string[] LastNames =
    [
        "Bakare", "Okafor", "Adeyemi", "Bello", "Nwosu", "Ogunleye", "Salami", "Eze", "Balogun", "Adebayo",
        "Lawal", "Okonkwo", "Alabi", "Ibrahim", "Fashola", "Oyelaran",
    ];

    private static readonly long[] BigPool = [1_000, 2_000, 5_000, 5_000, 10_000, 10_000, 20_000, 50_000, 100_000, 250_000];
    private static readonly long[] SmallPool = [1_000, 2_000, 5_000, 5_000, 10_000, 10_000, 20_000, 50_000];

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Donations.AnyAsync(d => d.Reference.StartsWith("CA-DON-DEMO-"), ct)) return;

        var now = DateTime.UtcNow;
        var existingIds = await db.Campaigns.Select(c => c.Id).ToListAsync(ct);
        var taken = new HashSet<string>(existingIds);

        var campaigns = new List<Campaign>();
        var donations = new List<Donation>();

        for (var i = 0; i < Plans.Length; i++)
        {
            var p = Plans[i];
            var id = p.Id;
            for (var n = 2; taken.Contains(id); n++) id = $"{p.Id}-{n}";
            taken.Add(id);

            var story = p.Story.ToList();
            if (DemoNote is not null) story.Add(DemoNote);

            var campaign = new Campaign
            {
                Id = id,
                Title = p.Title,
                Condition = p.Condition,
                Town = p.Town,
                Location = p.Location,
                ImageUrl = null,
                Preview = p.Preview,
                Story = story,
                Goal = p.Goal,
                Needs = p.Needs.ToList(),
                Funds = p.Funds.Select(f => new FundItem { Label = f.Label, Amount = f.Amount }).ToList(),
                OrganizerName = p.OrganizerName,
                OrganizerRole = p.OrganizerRole,
                Reviewed = true,
                Published = true,
                Featured = p.Featured,
                PublishedAt = now.AddDays(-p.PublishedDaysAgo),
            };

            foreach (var u in p.Updates)
            {
                campaign.Updates.Add(new CampaignUpdate
                {
                    CampaignId = id,
                    Date = DateOnly.FromDateTime(now.AddDays(-u.DaysAgo)),
                    Title = u.Title,
                    Text = u.Text,
                });
            }

            var rows = BuildDonations(id, p, i, now);
            campaign.Raised = rows.Sum(d => d.Amount);
            campaign.Supporters = rows.Count;
            campaigns.Add(campaign);
            donations.AddRange(rows);
        }

        // Reference numbers follow time order across all campaigns.
        donations = donations.OrderBy(d => d.CreatedAt).ToList();
        for (var n = 0; n < donations.Count; n++)
            donations[n].Reference = $"CA-DON-DEMO-{n + 1:0000}";

        db.Campaigns.AddRange(campaigns);
        db.Donations.AddRange(donations);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded {Campaigns} demo campaigns and {Donations} demo donations (fictional).",
            campaigns.Count, donations.Count);
    }

    private static List<Donation> BuildDonations(string campaignId, Plan p, int index, DateTime now)
    {
        var rng = new Random(1000 + index); // fixed seed: the same demo data every time
        var pool = p.Goal >= 1_000_000 ? BigPool : SmallPool;
        var window = Math.Max(1, p.PublishedDaysAgo) * 24 * 60; // minutes since publish

        var rows = new List<Donation>();
        var remaining = p.Raised;

        while (remaining > 0)
        {
            var amount = Math.Min(pool[rng.Next(pool.Length)], remaining);
            var left = remaining - amount;
            if (left > 0 && left < Lookups.MinimumDonation) amount = remaining; // never leave a sub-minimum remainder
            remaining -= amount;

            var anonymous = rng.NextDouble() < 0.25;
            var paidAt = now.AddMinutes(-rng.Next(5, window));
            var methodRoll = rng.NextDouble();

            rows.Add(new Donation
            {
                CampaignId = campaignId,
                Amount = amount,
                PlatformFee = 0,
                Total = amount,
                Name = anonymous ? null : $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}",
                Anonymous = anonymous,
                Email = $"demo-donor-{index + 1}-{rows.Count + 1}@example.com",
                Method = methodRoll < 0.6 ? PaymentMethod.Card : methodRoll < 0.9 ? PaymentMethod.Transfer : PaymentMethod.Ussd,
                Status = DonationStatus.Success,
                GatewayStatus = "success",
                PaidAt = paidAt,
                TestMode = true,
                CreatedAt = paidAt.AddSeconds(-rng.Next(20, 120)),
            });
        }

        return rows;
    }
}
