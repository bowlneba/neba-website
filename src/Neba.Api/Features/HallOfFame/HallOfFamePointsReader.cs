using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.HallOfFame;

/// <summary>
/// Loads the facts the Hall of Fame point calculation needs. Passing a bowler narrows each load to that bowler.
/// </summary>
internal sealed class HallOfFamePointsReader(AppDbContext appDbContext)
{
    private readonly IQueryable<Bowler> _bowlers = appDbContext.Bowlers.AsNoTracking();
    private readonly IQueryable<HallOfFameInduction> _inductions = appDbContext.HallOfFameInductions.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentChampion> _historicalChampions = appDbContext.HistoricalTournamentChampions.AsNoTracking();
    private readonly IQueryable<Tournament> _tournaments = appDbContext.Tournaments.AsNoTracking();
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<TitleWin>> LoadTitleWinsAsync(BowlerId? bowlerId, CancellationToken cancellationToken)
    {
        var historicalQuery = _historicalChampions
            .Select(champion => new
            {
                BowlerId = champion.Bowler.Id,
                champion.Tournament.SeasonId,
                champion.Tournament.StartDate,
                champion.Tournament.EndDate,
                champion.Tournament.TournamentType
            });

        // Titles recorded in this system (2019 on); earlier titles are the historical rows above.
        var recordedQuery = _tournaments
            .Where(tournament => tournament.TitleEligible)
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1)
                .Select(result => new
                {
                    result.BowlerId,
                    tournament.SeasonId,
                    tournament.StartDate,
                    tournament.EndDate,
                    tournament.TournamentType
                }));

        if (bowlerId is { } id)
        {
            historicalQuery = historicalQuery.Where(row => row.BowlerId == id);
            recordedQuery = recordedQuery.Where(row => row.BowlerId == id);
        }

        var historical = await historicalQuery.ToListAsync(cancellationToken);
        var recorded = await recordedQuery.ToListAsync(cancellationToken);

        return [.. historical.Concat(recorded)
            .Select(row => new TitleWin(row.BowlerId, row.SeasonId, row.StartDate, row.EndDate, row.TournamentType))];
    }

    public async Task<IReadOnlyCollection<AwardWin>> LoadAwardWinsAsync(BowlerId? bowlerId, CancellationToken cancellationToken)
    {
        var query = _seasons
            .SelectMany(season => season.BowlerOfTheYearAwards
                .Select(award => new { award.BowlerId, SeasonId = season.Id, season.EndDate, award.Category }));

        if (bowlerId is { } id)
        {
            query = query.Where(row => row.BowlerId == id);
        }

        var rows = await query.ToListAsync(cancellationToken);

        return [.. rows.Select(row => new AwardWin(row.BowlerId, row.SeasonId, row.EndDate, row.Category))];
    }

    public async Task<IReadOnlyCollection<MembershipRecord>> LoadMembershipsAsync(IReadOnlyCollection<BowlerId> bowlerIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bowlerIds);

        var rows = await _bowlers
            .Where(bowler => bowlerIds.Contains(bowler.Id))
            .SelectMany(bowler => bowler.Memberships
                .Select(membership => new { BowlerId = bowler.Id, membership.Type, membership.EndDate }))
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new MembershipRecord(row.BowlerId, row.Type, row.EndDate))];
    }

    /// <summary>Induction categories per inducted bowler, merged when a bowler has more than one induction.</summary>
    public async Task<IReadOnlyDictionary<BowlerId, IReadOnlyCollection<HallOfFameCategory>>> LoadInductedCategoriesAsync(CancellationToken cancellationToken)
    {
        var rows = await _inductions
            .Select(induction => new { induction.BowlerId, induction.Categories })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BowlerId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<HallOfFameCategory>)[.. group.SelectMany(row => row.Categories).Distinct()]);
    }

    /// <summary>Display names ("First Last") for the given bowlers.</summary>
    public async Task<IReadOnlyDictionary<BowlerId, string>> LoadNamesAsync(IReadOnlyCollection<BowlerId> bowlerIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bowlerIds);

        var rows = await _bowlers
            .Where(bowler => bowlerIds.Contains(bowler.Id))
            .Select(bowler => new { bowler.Id, bowler.Name })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.Name.ToDisplayName());
    }
}