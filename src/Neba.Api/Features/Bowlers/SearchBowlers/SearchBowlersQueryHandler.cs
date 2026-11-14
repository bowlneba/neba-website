using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Bowlers.SearchBowlers;

internal sealed class SearchBowlersQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>
{
    internal const int MaxResults = 20;

    // Said out loud in the query so the escaping below does not depend on the database's default.
    private const string EscapeCharacter = "\\";

    public async Task<IReadOnlyCollection<BowlerSearchSummary>> HandleAsync(
        SearchBowlersQuery query, CancellationToken cancellationToken)
    {
        // Escape the LIKE wildcards so "%" or "_" in the text match themselves.
        var pattern = "%"
            + query.Name.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal)
            + "%";

        // A bowler is found by "first last" or by "nickname last", the name the public pages show.
        return await appDbContext.Bowlers
            .AsNoTracking()
            .Where(bowler =>
                EF.Functions.ILike(bowler.Name.FirstName + " " + bowler.Name.LastName, pattern, EscapeCharacter)
                || (bowler.Name.Nickname != null && EF.Functions.ILike(bowler.Name.Nickname + " " + bowler.Name.LastName, pattern, EscapeCharacter)))
            .OrderBy(bowler => bowler.Name.LastName)
            .ThenBy(bowler => bowler.Name.FirstName)
            .Take(MaxResults)
            .Select(bowler => new BowlerSearchSummary
            {
                Id = bowler.Id,
                Name = bowler.Name,
                UsbcId = bowler.UsbcId
            })
            .ToListAsync(cancellationToken);
    }
}