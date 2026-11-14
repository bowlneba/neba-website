namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Applies the team composition rules (Rules 401 to 403) to a team and a tournament. It runs the rules at the
/// start date and the end date and combines the two answers.
/// </summary>
internal static class TeamEligibilityEvaluator
{
    private const int SeniorChampionAge = 60;

    /// <summary>Runs the rules at the start date and the end date and combines the two answers.</summary>
    public static TeamEligibilityResult Evaluate(TeamEligibilityContext context, IReadOnlyCollection<TeamMemberFacts> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        var atStart = EvaluateAt(context, context.StartDate, members);

        if (context.StartDate == context.EndDate)
        {
            return atStart.Eligible
                ? new TeamEligibilityResult(TeamEligibilityOutcome.Eligible, [], [])
                : new TeamEligibilityResult(TeamEligibilityOutcome.Ineligible, atStart.Reasons, []);
        }

        var atEnd = EvaluateAt(context, context.EndDate, members);

        if (atStart.Eligible && atEnd.Eligible)
        {
            return new TeamEligibilityResult(TeamEligibilityOutcome.Eligible, [], []);
        }

        // "Depends on the date" is for a team that is otherwise eligible. A team that fails the champion rules
        // on either date is ineligible, whatever its age answer is.
        var compositionFailed = atStart.CompositionFailed || atEnd.CompositionFailed;

        if ((!atStart.Eligible && !atEnd.Eligible) || compositionFailed)
        {
            return new TeamEligibilityResult(
                TeamEligibilityOutcome.Ineligible,
                [.. atStart.Reasons.Concat(atEnd.Reasons).DistinctBy(reason => reason.Code)],
                []);
        }

        return new TeamEligibilityResult(TeamEligibilityOutcome.DependsOnDate, [], [atStart, atEnd]);
    }

    /// <summary>Runs the rules in a fixed order for one date, stopping at the first group that fails: composition, then age.</summary>
    internal static TeamEligibilityCheck EvaluateAt(
        TeamEligibilityContext context, DateOnly asOf, IReadOnlyCollection<TeamMemberFacts> members)
    {
        var composition = CheckComposition(context, asOf, members);

        if (composition.Count > 0)
        {
            return new TeamEligibilityCheck(asOf, composition, CompositionFailed: true);
        }

        var age = OverUnderAgeLimit(context.TournamentType) is { } limit
            ? CheckAge(asOf, limit, members)
            : [];

        return new TeamEligibilityCheck(asOf, age);
    }

    private static List<TeamEligibilityReason> CheckComposition(
        TeamEligibilityContext context, DateOnly asOf, IReadOnlyCollection<TeamMemberFacts> members)
    {
        var titleCounts = members
            .Select(member => CountTitlesInWindow(context, asOf, member))
            .ToList();

        var champions = titleCounts.Count(count => count >= 1);
        var multiTime = titleCounts.Count(count => count > 1);
        var nonChampions = titleCounts.Count(count => count == 0);
        var reasons = new List<TeamEligibilityReason>();

        if (context.TournamentType == TournamentType.Doubles || OverUnderAgeLimit(context.TournamentType) is not null)
        {
            if (champions > 1)
            {
                reasons.Add(TeamEligibilityReasons.TooManyChampions(1));
            }
        }
        else if (context.TournamentType == TournamentType.Trios)
        {
            if (nonChampions < 1)
            {
                reasons.Add(TeamEligibilityReasons.NoNonChampion);
            }

            if (multiTime > 1)
            {
                reasons.Add(TeamEligibilityReasons.TooManyMultiTimeChampions(1));
            }
        }
        else if (context.TournamentType == TournamentType.Baker)
        {
            if (nonChampions < 1)
            {
                reasons.Add(TeamEligibilityReasons.NoNonChampion);
            }

            if (multiTime > 2)
            {
                reasons.Add(TeamEligibilityReasons.TooManyMultiTimeChampions(2));
            }

            if (members.Count(member => member.HallOfFameSuperiorPerformance) > 1)
            {
                reasons.Add(TeamEligibilityReasons.TooManyHallOfFame);
            }
        }

        return reasons;
    }

    // Titles won in tournaments that overlap this tournament's dates do not count (Rule 401a, same weekend);
    // neither does this tournament's own title. Titles after the as-of date never count.
    private static int CountTitlesInWindow(TeamEligibilityContext context, DateOnly asOf, TeamMemberFacts member)
    {
        var windowStart = asOf.AddYears(-WindowYears(context.TournamentType, asOf, member.DateOfBirth));

        return member.Titles.Count(title =>
            title.EndDate >= windowStart
            && title.EndDate <= asOf
            && !(title.StartDate <= context.EndDate && title.EndDate >= context.StartDate));
    }

    // Regular doubles: 10 years, or 5 for a bowler over 60. Everything else: 5 years.
    // A missing date of birth means "not over 60", so the longer window applies.
    private static int WindowYears(TournamentType type, DateOnly asOf, DateOnly? dateOfBirth)
        => type == TournamentType.Doubles && !IsOver(dateOfBirth, asOf, SeniorChampionAge) ? 10 : 5;

    private static List<TeamEligibilityReason> CheckAge(DateOnly asOf, int ageLimit, IReadOnlyCollection<TeamMemberFacts> members)
    {
        if (members.Any(member => member.DateOfBirth is null))
        {
            return [TeamEligibilityReasons.AgeNotVerified];
        }

        var overs = members.Count(member => IsOver(member.DateOfBirth, asOf, ageLimit));
        var unders = members.Count - overs;

        return overs == 1 && unders == 1
            ? []
            : [TeamEligibilityReasons.SameAgeGroup];
    }

    // True when the bowler has reached the given age by the as-of date; the birthday itself counts.
    private static bool IsOver(DateOnly? dateOfBirth, DateOnly asOf, int age)
        => dateOfBirth is { } birthDate && birthDate.AddYears(age) <= asOf;

    // The age that splits an over/under team, or null when the tournament is not an over/under format.
    private static int? OverUnderAgeLimit(TournamentType type)
    {
        if (type == TournamentType.OverUnderFiftyDoubles)
        {
            return 50;
        }

        return type == TournamentType.OverUnderFortyDoubles ? 40 : null;
    }
}
