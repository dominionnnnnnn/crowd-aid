using CrowdAid.Data;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>Public campaign browsing (spec #1, #2). Published campaigns only, no private data.</summary>
public class CampaignService(AppDbContext db) : ICampaignService
{
    public async Task<CampaignListResponse> ListAsync(string? q, string? location, string? need, bool? featured,
        CancellationToken ct = default)
    {
        var published = db.Campaigns.AsNoTracking().Where(c => c.Published);
        var query = published;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = "%" + EscapeLike(q.Trim()) + "%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, pattern) ||
                EF.Functions.ILike(c.Condition, pattern) ||
                EF.Functions.ILike(c.Town, pattern));
        }
        if (!string.IsNullOrWhiteSpace(location) && location != "all")
            query = query.Where(c => c.Town == location);
        if (!string.IsNullOrWhiteSpace(need) && need != "all")
            query = query.Where(c => c.Needs.Contains(need));
        if (featured == true)
            query = query.Where(c => c.Featured);

        var cards = await query
            .OrderByDescending(c => c.Featured)
            .ThenByDescending(c => c.PublishedAt)
            .Select(c => new CampaignCardDto(
                c.Id, c.Title, c.Condition, c.Town, c.Location, c.ImageUrl, c.Preview,
                c.Raised, c.Goal, c.Supporters, c.Needs, c.Reviewed))
            .ToListAsync(ct);

        // Towns of all published campaigns, for the Location filter (not just this result set).
        var locations = await published.Select(c => c.Town).Distinct().OrderBy(t => t).ToListAsync(ct);

        return new CampaignListResponse(cards, locations);
    }

    public async Task<CampaignDetailDto> GetPublishedAsync(string id, CancellationToken ct = default)
    {
        var c = await db.Campaigns.AsNoTracking()
            .Include(x => x.Updates)
            .FirstOrDefaultAsync(x => x.Id == id && x.Published, ct)
            ?? throw new NotFoundException("This campaign isn't available.");

        return ToDetail(c);
    }

    internal static CampaignDetailDto ToDetail(Campaign c) => new(
        c.Id, c.Title, c.Condition, c.Town, c.Location, c.Reviewed,
        c.Raised, c.Goal, c.Supporters,
        c.Story, c.Needs,
        c.Funds.Select(f => new FundItemDto(f.Label, f.Amount)).ToList(),
        c.Updates.OrderByDescending(u => u.Date).Select(u => new CampaignUpdateDto(u.Date, u.Title, u.Text)).ToList(),
        new OrganizerDto(c.OrganizerName, c.OrganizerRole));

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
