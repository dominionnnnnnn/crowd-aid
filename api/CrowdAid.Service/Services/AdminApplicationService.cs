using System.Text.RegularExpressions;
using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Admin;
using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>
/// Admin review: list, open, verify, decide, publish, resend activation (spec #13–17, #33).
/// Every status change is a guarded UPDATE (WHERE status is one of the allowed ones), so two admins
/// or a double click can't both succeed.
/// </summary>
public partial class AdminApplicationService(AppDbContext db, ActivationLinkService links) : IAdminApplicationService
{
    private static readonly string[] Locked =
        [ApplicationStatus.Approved, ApplicationStatus.Published, ApplicationStatus.Rejected];

    private static ConflictException NotAllowed() => new("INVALID_STATUS",
        "This action is not allowed in the current status. Refresh and try again.");

    // Application support types -> public campaign needs (spec #17). Financial support is not a "need" item.
    private static readonly Dictionary<string, string> NeedFor = new()
    {
        ["Transportation"] = "Transport to hospital",
        ["Blood donor coordination"] = "Blood donors",
        ["Food"] = "Meals for family",
        ["Professional support"] = "Professional support",
        ["Medical equipment"] = "Medical equipment",
    };

    // ---- #13 list ----

    public async Task<ListResponse<AdminApplicationRowDto>> ListAsync(string? status, CancellationToken ct = default)
    {
        var query = db.Applications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status.Trim());

        var rows = await query
            .OrderByDescending(a => a.ReceivedAt)
            .Select(a => new AdminApplicationRowDto(
                a.Reference, a.Name, a.PatientName, a.Relationship, a.Town, a.Treatment, a.AmountNeeded,
                a.Status, a.Files.Any(), a.ReceivedAt, a.Status == ApplicationStatus.Received))
            .ToListAsync(ct);

        return new ListResponse<AdminApplicationRowDto>(rows);
    }

    // ---- #14 detail (opening a Received application moves it to In review) ----

    public async Task<AdminApplicationDetailDto> GetAsync(string reference, string adminName, CancellationToken ct = default)
    {
        var app = await LoadAsync(reference, ct);

        if (app.Status == ApplicationStatus.Received)
        {
            var moved = await db.Applications
                .Where(a => a.Id == app.Id && a.Status == ApplicationStatus.Received)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, ApplicationStatus.InReview), ct);
            if (moved == 1)
            {
                app.Status = ApplicationStatus.InReview;
                db.ApplicationLogs.Add(NewLog(app.Id, "Opened", adminName));
                await db.SaveChangesAsync(ct);
            }
        }

        return await BuildDetailAsync(app, ct);
    }

    // ---- #15 verification ----

    public async Task<AdminApplicationDetailDto> UpdateVerificationAsync(
        string reference, VerificationRequest r, string adminName, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var status = v.OneOf("status", r.Status, VerificationStatus.All, "Choose a valid verification status.");
        var method = v.Optional("method", r.Method, 1000);
        v.ThrowIfInvalid();

        var app = await LoadAsync(reference, ct);
        if (Locked.Contains(app.Status)) throw NotAllowed();

        // Verified also moves the application to Verified; un-verifying moves it back to In review.
        // "More info requested" is left alone: the admin marks info received first.
        var next = app.Status;
        if (status == VerificationStatus.Verified && app.Status is ApplicationStatus.Received or ApplicationStatus.InReview)
            next = ApplicationStatus.Verified;
        else if (status != VerificationStatus.Verified && app.Status == ApplicationStatus.Verified)
            next = ApplicationStatus.InReview;

        var changed = await db.Applications
            .Where(a => a.Id == app.Id && !Locked.Contains(a.Status))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, next)
                .SetProperty(a => a.VerificationStatus, status)
                .SetProperty(a => a.VerificationMethod, method), ct);
        if (changed == 0) throw NotAllowed();

        db.ApplicationLogs.Add(NewLog(app.Id, "Verification updated", adminName, note: method));
        await db.SaveChangesAsync(ct);
        await db.Entry(app).ReloadAsync(ct);

        return await BuildDetailAsync(app, ct);
    }

    // ---- #16 actions ----

    public async Task<ApplicationActionResponse> ActionAsync(
        string reference, ApplicationActionRequest r, string adminName, Guid adminId, CancellationToken ct = default)
    {
        var action = r.Action?.Trim();
        var v = new FormValidator();
        string? needed = null, reason = null, message = null;
        var note = v.Optional("note", r.Note, 1000);

        switch (action)
        {
            case "request_info":
                needed = v.Required("needed", r.Needed, "This field is required.", 1000);
                message = v.Required("message", r.Message, "This field is required.", 2000);
                break;
            case "reject":
                reason = v.Required("reason", r.Reason, "This field is required.", 1000);
                message = v.Required("message", r.Message, "This field is required.", 2000);
                break;
            case "info_received":
            case "approve":
                break;
            default:
                v.Add("action", "Choose a valid action.");
                break;
        }
        v.ThrowIfInvalid();

        var app = await LoadAsync(reference, ct);

        string[] allowedFrom;
        string to;
        string logAction;
        switch (action)
        {
            case "request_info":
                allowedFrom = [ApplicationStatus.InReview, ApplicationStatus.Verified];
                to = logAction = ApplicationStatus.MoreInfoRequested;
                break;
            case "info_received":
                allowedFrom = [ApplicationStatus.MoreInfoRequested];
                to = ApplicationStatus.InReview;
                logAction = "Information received";
                break;
            case "reject":
                allowedFrom = [ApplicationStatus.InReview, ApplicationStatus.Verified, ApplicationStatus.MoreInfoRequested];
                to = logAction = ApplicationStatus.Rejected;
                break;
            default: // approve
                allowedFrom = [ApplicationStatus.Verified];
                to = logAction = ApplicationStatus.Approved;
                break;
        }

        if (!allowedFrom.Contains(app.Status)) throw NotAllowed();
        if (action == "approve" && app.VerificationStatus != VerificationStatus.Verified) throw NotAllowed();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var moved = await db.Applications
            .Where(a => a.Id == app.Id && allowedFrom.Contains(a.Status))
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, to), ct);
        if (moved == 0) throw NotAllowed();
        app.Status = to;

        db.ApplicationLogs.Add(NewLog(app.Id, logAction, adminName, note, needed ?? reason, message));

        // Approve: find or create the campaigner and issue a one-time link (steps 1–4, one transaction).
        User? user = null;
        IssuedLink? issued = null;
        if (action == "approve")
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == app.Email, ct);
            if (existing is { Role: UserRole.Admin })
                throw new ConflictException("EMAIL_IN_USE",
                    "This email belongs to an admin account. Ask the applicant for a different email.");

            user = existing ?? new User { Name = app.Name, Email = app.Email, Phone = app.Phone, Role = UserRole.Campaigner };
            if (existing is null) db.Users.Add(user);
            app.UserId = user.Id;

            // Someone who already set a password (second application, same email) needs no new link.
            if (user.PasswordHash is null) issued = await links.CreateAsync(user, adminId, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        if (user is null) return new ApplicationActionResponse(app.Reference, to, null);

        // Email only after the commit. A failed send never undoes the approval.
        bool? emailSent = issued is null ? null : await links.SendAsync(user, app, issued, ct);
        var account = new AccountLinkDto(
            issued is null ? AccountState.Activated : AccountState.LinkSent,
            user.Email, app.Phone, issued?.Link, issued?.ExpiresAt, emailSent);

        return new ApplicationActionResponse(app.Reference, to, account);
    }

    // ---- #17 publish ----

    public async Task<PublishResponse> PublishAsync(string reference, PublishRequest r, string adminName, CancellationToken ct = default)
    {
        var v = new FormValidator();
        var title = v.Required("title", r.Title, "Enter a public title.", 150);
        var location = v.Required("location", r.Location, "Enter the public location.", 150);
        if (r.Goal is not > 0) v.Add("goal", "Enter a positive whole naira amount.");
        var organizer = v.Optional("organizer", r.Organizer, 150);
        var story = v.Required("story", r.Story, "Write the public story.", 6000);
        var consentRecorded = v.Required("consentRecorded", r.ConsentRecorded, "Record when and how consent was given.", 200);
        v.MustBeTrue("publicationConsent", r.PublicationConsent, "Confirm publication consent was obtained.");
        v.MustBeTrue("contentReviewed", r.ContentReviewed, "Confirm the public content was reviewed.");
        v.ThrowIfInvalid();
        var goal = r.Goal!.Value;

        var app = await LoadAsync(reference, ct);
        if (app.Status != ApplicationStatus.Approved) throw NotAllowed();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var moved = await db.Applications
            .Where(a => a.Id == app.Id && a.Status == ApplicationStatus.Approved)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, ApplicationStatus.Published), ct);
        if (moved == 0) throw NotAllowed();
        app.Status = ApplicationStatus.Published;

        var slug = await UniqueSlugAsync(app.PatientName, ct);
        var paragraphs = ParagraphBreak().Split(story).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        var (organizerName, organizerRole) = SplitOrganizer(organizer, app);

        // Only the public fields go onto the campaign. Contact details and documents never do.
        db.Campaigns.Add(new Campaign
        {
            Id = slug,
            ApplicationReference = app.Reference,
            OwnerUserId = app.UserId,
            Title = title,
            Condition = app.Treatment,
            Town = app.Town,
            Location = location,
            Preview = Preview(paragraphs[0]),
            Story = paragraphs,
            Raised = 0,
            Goal = goal,
            Supporters = 0,
            Needs = app.SupportTypes.Where(NeedFor.ContainsKey).Select(t => NeedFor[t]).ToList(),
            Funds = [new FundItem { Label = "Treatment and care", Amount = goal }],
            OrganizerName = organizerName,
            OrganizerRole = organizerRole,
            Reviewed = true,
            Published = true,
            Featured = false,
            PublishedAt = DateTime.UtcNow,
        });

        app.Publication = new Publication
        {
            Title = title, Location = location, Goal = goal, Organizer = organizer, Story = story,
            ConsentRecorded = consentRecorded, PublicationConsent = true, ContentReviewed = true,
        };
        app.CampaignId = slug;
        db.ApplicationLogs.Add(NewLog(app.Id, "Published", adminName));

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new PublishResponse(slug, ApplicationStatus.Published);
    }

    // ---- #33 resend / reset activation link ----

    public async Task<AccountLinkDto> ResendActivationAsync(string reference, string adminName, Guid adminId, CancellationToken ct = default)
    {
        var app = await LoadAsync(reference, ct);
        if (app.Status is not (ApplicationStatus.Approved or ApplicationStatus.Published) || app.UserId is null)
            throw NotAllowed();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == app.UserId, ct) ?? throw NotAllowed();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var issued = await links.CreateAsync(user, adminId, ct);
        db.ApplicationLogs.Add(NewLog(app.Id, "Activation link re-sent", adminName));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var sent = await links.SendAsync(user, app, issued, ct);
        return new AccountLinkDto(AccountState.LinkSent, user.Email, app.Phone, issued.Link, issued.ExpiresAt, sent);
    }

    // ---- helpers ----

    private async Task<Application> LoadAsync(string reference, CancellationToken ct)
    {
        var refValue = reference.Trim().ToUpperInvariant();
        return await db.Applications.Include(a => a.Files).FirstOrDefaultAsync(a => a.Reference == refValue, ct)
            ?? throw new NotFoundException("Application not found.");
    }

    private static ApplicationLog NewLog(Guid applicationId, string action, string by,
        string? note = null, string? reason = null, string? message = null)
        => new() { ApplicationId = applicationId, Action = action, By = by, Note = note, Reason = reason, Message = message };

    private async Task<AdminApplicationDetailDto> BuildDetailAsync(Application a, CancellationToken ct)
    {
        var logs = await db.ApplicationLogs.AsNoTracking()
            .Where(l => l.ApplicationId == a.Id)
            .OrderBy(l => l.At)
            .ToListAsync(ct);

        var account = new AccountSummaryDto(AccountState.NotCreated, null, null);
        if (a.UserId is { } userId)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is not null)
            {
                var expires = await db.ActivationTokens.AsNoTracking()
                    .Where(t => t.UserId == userId && t.UsedAt == null && t.RevokedAt == null)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (DateTime?)t.ExpiresAt)
                    .FirstOrDefaultAsync(ct);
                account = new AccountSummaryDto(
                    user.ActivatedAt != null ? AccountState.Activated : AccountState.LinkSent, expires, user.ActivatedAt);
            }
        }

        var p = a.Publication;
        return new AdminApplicationDetailDto(
            a.Reference, a.ReceivedAt, a.Who, a.Name, a.Phone, a.Email, a.Relationship, a.PatientName, a.PatientAge,
            a.Town, a.Lga, a.Condition, a.Treatment, a.Facility, a.EstimatedCost, a.AmountNeeded, a.SupportTypes,
            a.PrivateReviewConsent, a.AccuracyConfirmed, a.Status,
            new VerificationDto(a.VerificationStatus, a.VerificationMethod ?? ""),
            a.Files.Select(f => f.FileName).ToList(),
            logs.Select(l => new ApplicationLogDto(l.Action, l.By, l.At, JoinNote(l))).ToList(),
            p is null ? null : new PublicationDto(p.Title, p.Location, p.Goal, p.Organizer, p.Story,
                p.ConsentRecorded, p.PublicationConsent, p.ContentReviewed),
            a.CampaignId,
            account);
    }

    private static string? JoinNote(ApplicationLog l)
    {
        var parts = new[] { l.Note, l.Reason, l.Message }.Where(s => !string.IsNullOrWhiteSpace(s));
        var text = string.Join("\n", parts);
        return text.Length == 0 ? null : text;
    }

    private async Task<string> UniqueSlugAsync(string patientName, CancellationToken ct)
    {
        var first = patientName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var baseSlug = new string(first.ToLowerInvariant().Where(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9')).ToArray());
        if (baseSlug.Length == 0) baseSlug = "campaign";

        var slug = baseSlug;
        for (var n = 2; await db.Campaigns.AnyAsync(c => c.Id == slug, ct); n++)
            slug = $"{baseSlug}-{n}";
        return slug;
    }

    /// <summary>"Aminat Yusuf · Usman's aunt" -> name and role. Blank organizer falls back to the applicant.</summary>
    private static (string Name, string Role) SplitOrganizer(string? organizer, Application app)
    {
        if (!string.IsNullOrWhiteSpace(organizer))
        {
            var parts = organizer.Split('·', 2, StringSplitOptions.TrimEntries);
            return (parts[0], parts.Length > 1 ? parts[1] : "");
        }
        return (app.Name, app.Who == ApplicantWho.Myself ? "Patient" : app.Relationship);
    }

    private static string Preview(string firstParagraph)
        => firstParagraph.Length <= 140 ? firstParagraph : firstParagraph[..140].TrimEnd() + "…";

    [GeneratedRegex(@"\r?\n\s*\r?\n")]
    private static partial Regex ParagraphBreak();
}
