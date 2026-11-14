using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal sealed class CheckTeamEligibilityQueryHandler(
    AppDbContext appDbContext,
    DefendingTeamsReader defendingTeamsReader,
    BowlerTitlesReader titlesReader)
    : IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>
{
    public async Task<ErrorOr<TeamEligibilityDto>> HandleAsync(
        CheckTeamEligibilityQuery query, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(t => t.Id == query.TournamentId)
            .Select(t => new { t.TournamentType, t.StartDate, t.EndDate, t.PreviousTournamentId })
            .SingleOrDefaultAsync(cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(query.TournamentId);
        }

        var teamSize = tournament.TournamentType.TeamSize;

        if (teamSize < 2)
        {
            return TeamEligibilityErrors.NotATeamTournament(query.TournamentId);
        }

        if (query.BowlerIds.Count + query.UnknownBowlerCount != teamSize)
        {
            return TeamEligibilityErrors.TeamSizeMismatch(teamSize);
        }

        // 1. Defending champions: the whole team must equal one defending team (the previous tournament's winners,
        //    plus the winners before it while the chain is truncated). Skipped when any bowler is unknown, since an
        //    unknown bowler cannot be a past winner.
        if (query.UnknownBowlerCount == 0 && tournament.PreviousTournamentId is not null)
        {
            var defending = await defendingTeamsReader.LoadAsync([query.TournamentId], cancellationToken);

            var matched = defending.GetValueOrDefault(query.TournamentId, [])
                .FirstOrDefault(team => team.Champions.Count == teamSize
                    && team.Champions.Select(champion => champion.BowlerId).ToHashSet().SetEquals(query.BowlerIds));

            if (matched is not null)
            {
                return new TeamEligibilityDto(TeamEligibilityOutcome.DefendingChampions, [], [], matched);
            }
        }

        // 2. Facts per member. A bowler ID that does not exist is a 404.
        var bowlerIds = query.BowlerIds.ToArray();

        var bowlers = await appDbContext.Bowlers
            .AsNoTracking()
            .Where(bowler => bowlerIds.Contains(bowler.Id))
            .Select(bowler => new { bowler.Id, bowler.DateOfBirth, bowler.Name, bowler.UsbcId })
            .ToListAsync(cancellationToken);

        var knownIds = bowlers.Select(bowler => bowler.Id).ToHashSet();
        var missing = query.BowlerIds.Where(id => !knownIds.Contains(id)).Cast<BowlerId?>().FirstOrDefault();

        if (missing is { } missingId)
        {
            return BowlerErrors.NotFound(missingId);
        }

        // Categories is a flag-enum list; it is read in memory, not translated to SQL.
        var superiorPerformers = (await appDbContext.HallOfFameInductions
                .AsNoTracking()
                .Where(induction => bowlerIds.Contains(induction.BowlerId))
                .ToListAsync(cancellationToken))
            .Where(induction => induction.Categories.Contains(HallOfFameCategory.SuperiorPerformance))
            .Select(induction => induction.BowlerId)
            .ToHashSet();

        var titles = await titlesReader.LoadAsync(query.BowlerIds, cancellationToken);

        // In the order the bowlers were given, then members not in the system.
        var orderedBowlers = query.BowlerIds
            .Select(id => bowlers.Single(bowler => bowler.Id == id))
            .ToList();

        var members = orderedBowlers
            .Select(bowler => new TeamMemberFacts
            {
                BowlerId = bowler.Id,
                DateOfBirth = bowler.DateOfBirth,
                Titles = [.. titles[bowler.Id]],
                HallOfFameSuperiorPerformance = superiorPerformers.Contains(bowler.Id)
            })
            .Concat(Enumerable.Range(0, query.UnknownBowlerCount).Select(_ => new TeamMemberFacts()))
            .ToList();

        // 3. Composition, then age, at both dates (the evaluator owns the rules and how the dates combine).
        var context = new TeamEligibilityContext(tournament.TournamentType, tournament.StartDate, tournament.EndDate);
        var result = TeamEligibilityEvaluator.Evaluate(context, members);

        // The facts behind the answer go with every answer, so a bowler can see what decided it.
        var explanation = TeamEligibilityEvaluator.Explain(context, members);
        var identities = orderedBowlers
            .Select(bowler => new TeamEligibilityMemberIdentity(bowler.Name, bowler.UsbcId))
            .Concat(Enumerable.Range(0, query.UnknownBowlerCount).Select(_ => new TeamEligibilityMemberIdentity(null, null)))
            .ToList();

        return new TeamEligibilityDto(
            result.Outcome, result.Reasons, result.DateChecks, Explanation: explanation, MemberIdentities: identities);
    }
}