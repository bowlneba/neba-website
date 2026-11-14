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

        return (!atStart.Eligible && !atEnd.Eligible) || compositionFailed
            ? new TeamEligibilityResult(
                TeamEligibilityOutcome.Ineligible,
                [.. atStart.Reasons.Concat(atEnd.Reasons).DistinctBy(reason => reason.Code)],
                [])
            : new TeamEligibilityResult(TeamEligibilityOutcome.DependsOnDate, [], [atStart, atEnd]);
    }

    /// <summary>
    /// Lists what the rules look at for each member, and the tally of each rule, at the start date. It never
    /// carries a date of birth. Both title windows show for doubles, so a bowler's window does not tell the
    /// reader who has turned 60.
    /// </summary>
    public static TeamEligibilityExplanation Explain(TeamEligibilityContext context, IReadOnlyCollection<TeamMemberFacts> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        var isDoubles = context.TournamentType == TournamentType.Doubles;
        var hasAgeRule = OverUnderAgeLimit(context.TournamentType) is not null;

        var explained = ExplainMembers(context, members, isDoubles, hasAgeRule);

        var champions = explained.Count(member => member.CountsAs != ChampionStanding.NonChampion);
        var multiTime = explained.Count(member => member.CountsAs == ChampionStanding.MultiTimeChampion);
        var nonChampions = explained.Count(member => member.CountsAs == ChampionStanding.NonChampion);
        var verifiedCount = members.Count(member => member.DateOfBirth is not null);
        var hallOfFameCount = members.Count(member => member.HallOfFameSuperiorPerformance);

        var checks = BuildChecks(context.TournamentType, champions, multiTime, nonChampions, hallOfFameCount);

        if (hasAgeRule)
        {
            checks.Add(new(TeamEligibilityReasons.AgeCheck(verifiedCount, members.Count), verifiedCount == members.Count));
        }

        // Mark the members whose facts count against a rule the team failed, so the page can show which ones.
        var titlesFailed = FailedTitleRule(context.TournamentType, champions, multiTime, nonChampions);
        var ageFailed = hasAgeRule && verifiedCount != members.Count;
        var hallOfFameFailed = context.TournamentType == TournamentType.Baker && hallOfFameCount > 1;
        var marked = explained
            .ConvertAll(member => member with
            {
                TitlesBreakRule = TitlesBreakRule(titlesFailed, member),
                AgeBreaksRule = ageFailed && member.AgeVerified == false,
                HallOfFameBreaksRule = hallOfFameFailed && member.HallOfFameSuperiorPerformance == true
            })
;

        var dates = context.StartDate == context.EndDate
            ? $"The date checked is {FormatDate(context.StartDate)}."
            : $"The dates checked are {FormatDate(context.StartDate)} and {FormatDate(context.EndDate)}.";

        return new TeamEligibilityExplanation(
            TeamEligibilityReasons.WindowSummary(context.TournamentType.Name, isDoubles ? 10 : 5, isDoubles, dates),
            marked,
            checks);
    }

    private static List<TeamMemberExplanation> ExplainMembers(
        TeamEligibilityContext context, IReadOnlyCollection<TeamMemberFacts> members, bool isDoubles, bool hasAgeRule)
    {
        var asOf = context.StartDate;

        return [.. members.Select(member => new TeamMemberExplanation(
            member,
            CountTitles(context, asOf, member, 5),
            isDoubles ? CountTitles(context, asOf, member, 10) : null,
            StandingFor(CountTitlesInWindow(context, asOf, member)),
            member.Titles.Count == 0 ? null : member.Titles.Max(title => title.EndDate.Year),
            hasAgeRule ? member.DateOfBirth is not null : null,
            context.TournamentType == TournamentType.Baker ? member.HallOfFameSuperiorPerformance : null))];
    }

    private static ChampionStanding StandingFor(int titlesInWindow)
        => titlesInWindow switch
        {
            0 => ChampionStanding.NonChampion,
            1 => ChampionStanding.OneTimeChampion,
            _ => ChampionStanding.MultiTimeChampion
        };

    private static List<TeamEligibilityCheckLine> BuildChecks(
        TournamentType type, int champions, int multiTime, int nonChampions, int hallOfFame)
    {
        if (type == TournamentType.Doubles || OverUnderAgeLimit(type) is not null)
        {
            return [new(TeamEligibilityReasons.ChampionsCheck(champions, 1), champions <= 1)];
        }

        if (type == TournamentType.Trios)
        {
            return
            [
                new(TeamEligibilityReasons.NonChampionsCheck(nonChampions), nonChampions >= 1),
                new(TeamEligibilityReasons.MultiTimeCheck(multiTime, 1), multiTime <= 1)
            ];
        }

        return type == TournamentType.Baker
            ? [
                new(TeamEligibilityReasons.NonChampionsCheck(nonChampions), nonChampions >= 1),
                new(TeamEligibilityReasons.MultiTimeCheck(multiTime, 2), multiTime <= 2),
                new(TeamEligibilityReasons.HallOfFameCheck(hallOfFame), hallOfFame <= 1)
            ]
            : [];
    }

    private static bool TitlesBreakRule(TitleRuleFailure failure, TeamMemberExplanation member)
        => failure switch
        {
            TitleRuleFailure.Champions => member.CountsAs != ChampionStanding.NonChampion,
            TitleRuleFailure.MultiTime => member.CountsAs == ChampionStanding.MultiTimeChampion,
            TitleRuleFailure.NoNonChampion => true,
            _ => false
        };

    private enum TitleRuleFailure
    {
        None,
        Champions,
        MultiTime,
        NoNonChampion
    }

    // The first champion rule the team failed, for marking the members whose titles count against it.
    private static TitleRuleFailure FailedTitleRule(TournamentType type, int champions, int multiTime, int nonChampions)
    {
        if (type == TournamentType.Doubles || OverUnderAgeLimit(type) is not null)
        {
            return champions > 1 ? TitleRuleFailure.Champions : TitleRuleFailure.None;
        }

        if (type != TournamentType.Trios && type != TournamentType.Baker)
        {
            return TitleRuleFailure.None;
        }

        var maxMultiTime = type == TournamentType.Baker ? 2 : 1;

        return (multiTime > maxMultiTime, nonChampions < 1) switch
        {
            (true, _) => TitleRuleFailure.MultiTime,
            (_, true) => TitleRuleFailure.NoNonChampion,
            _ => TitleRuleFailure.None
        };
    }

    private static string FormatDate(DateOnly date)
        => date.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

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
        var type = context.TournamentType;

        if (type == TournamentType.Doubles || OverUnderAgeLimit(type) is not null)
        {
            return champions > 1 ? [TeamEligibilityReasons.TooManyChampions(1)] : [];
        }

        if (type == TournamentType.Trios)
        {
            return CheckNonChampionsAndMultiTime(nonChampions, multiTime, 1);
        }

        if (type == TournamentType.Baker)
        {
            var reasons = CheckNonChampionsAndMultiTime(nonChampions, multiTime, 2);

            if (members.Count(member => member.HallOfFameSuperiorPerformance) > 1)
            {
                reasons.Add(TeamEligibilityReasons.TooManyHallOfFame);
            }

            return reasons;
        }

        return [];
    }

    private static List<TeamEligibilityReason> CheckNonChampionsAndMultiTime(int nonChampions, int multiTime, int maxMultiTime)
    {
        var reasons = new List<TeamEligibilityReason>();

        if (nonChampions < 1)
        {
            reasons.Add(TeamEligibilityReasons.NoNonChampion);
        }

        if (multiTime > maxMultiTime)
        {
            reasons.Add(TeamEligibilityReasons.TooManyMultiTimeChampions(maxMultiTime));
        }

        return reasons;
    }

    // Titles won in tournaments that overlap this tournament's dates do not count (Rule 401a, same weekend);
    // neither does this tournament's own title. Titles after the as-of date never count.
    private static int CountTitlesInWindow(TeamEligibilityContext context, DateOnly asOf, TeamMemberFacts member)
        => CountTitles(context, asOf, member, WindowYears(context.TournamentType, asOf, member.DateOfBirth));

    private static int CountTitles(TeamEligibilityContext context, DateOnly asOf, TeamMemberFacts member, int years)
    {
        var windowStart = asOf.AddYears(-years);

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
        => OverUnderAgeLimits.TryGetValue(type, out var limit) ? limit : null;

    private static readonly Dictionary<TournamentType, int> OverUnderAgeLimits = new()
    {
        [TournamentType.OverUnderFiftyDoubles] = 50,
        [TournamentType.OverUnderFortyDoubles] = 40
    };
}