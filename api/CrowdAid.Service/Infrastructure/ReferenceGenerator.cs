using CrowdAid.Data;
using CrowdAid.Data.IRepo;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Infrastructure;

/// <summary>
/// References come from Postgres sequences, never from a row count, so two requests at the
/// same moment can't get the same number. Sequence numbers can skip; that's fine.
/// </summary>
public class ReferenceGenerator(AppDbContext db) : IReferenceGenerator
{
    public async Task<string> NextAsync(string prefix, CancellationToken ct = default)
    {
        if (!AppDbContext.ReferenceSequences.TryGetValue(prefix, out var sequence))
            throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Unknown reference prefix");

        // Sequence name comes from the fixed dictionary above, never from user input.
        var next = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{sequence}') AS \"Value\"")
            .SingleAsync(ct);

        return $"{prefix}-{next:D4}";
    }
}
