using Neba.Api.Features.Stats.BoyProgression;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;

namespace Neba.Api.Features.Stats.GetSeasonStats;

internal sealed class BowlerOfTheYearRaceCalculator
    : IBowlerOfTheYearRaceCalculator
{
    public IReadOnlyDictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> CalculateAllProgressions(IReadOnlyCollection<BoyProgressionResultDto> results)
        => new Dictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
        {
            [BowlerOfTheYearCategory.Open.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Open),
            [BowlerOfTheYearCategory.Senior.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Senior),
            [BowlerOfTheYearCategory.SuperSenior.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.SuperSenior),
            [BowlerOfTheYearCategory.Woman.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Woman),
            [BowlerOfTheYearCategory.Youth.Value] = ComputeRaceProgression(results, BowlerOfTheYearCategory.Youth),
            [BowlerOfTheYearCategory.Rookie.Value] = [],  // Deferred: requires membership data
        };

    private static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> ComputeRaceProgression(
        IReadOnlyCollection<BoyProgressionResultDto> results,
        BowlerOfTheYearCategory category)
    {
        var eligible = results
            .Where(result => IsTournamentEligibleForRace(result, category) && IsBowlerEligibleForRace(result, category))
            .ToList();

        if (eligible.Count == 0)
        {
            return [];
        }

        // All tournaments for this race in chronological order — shared X-axis for every series.
        // Deduplicate names: when two tournaments share a display name, append the date so each
        // chart category label is unique (duplicate names cause ApexCharts to drop data points).
        var allTournaments = eligible
            .GroupBy(result => result.TournamentId)
            .Select(group => (Id: group.Key, Name: group.First().TournamentName, Date: group.First().TournamentDate))
            .OrderBy(tournament => tournament.Date)
            .ToArray();

        var duplicateNames = allTournaments
            .GroupBy(tournament => tournament.Name)
            .Where(group => group.Count() > 1)
            .SelectMany(tournament => tournament)
            .Select(tournament => tournament.Id)
            .ToHashSet();

        allTournaments = [.. allTournaments.Select(tournament =>
            duplicateNames.Contains(tournament.Id)
                ? (tournament.Id, $"{tournament.Name} ({tournament.Date:M/d})", tournament.Date)
                : tournament)];

        var byBowler = eligible.GroupBy(result => result.BowlerId);

        return [.. byBowler.Select(group =>
        {
            // Collapse multiple rows for the same tournament into one points value.
            var pointsByTournament = group
                .GroupBy(result => result.TournamentId)
                .ToDictionary(group => group.Key, group => group.Sum(result => PointsForRace(result, category)));

            var cumulativePoints = 0;
            var tournamentResults = allTournaments.Select(tournament =>
            {
                // Tournaments the bowler didn't enter contribute 0 points — line stays flat.
                if (pointsByTournament.TryGetValue(tournament.Id, out var pts)) 
                {
                    cumulativePoints += pts;
                }
                
                return new BowlerOfTheYearPointsRaceTournamentDto
                {
                    TournamentName = tournament.Name,
                    TournamentDate = tournament.Date,
                    CumulativePoints = cumulativePoints
                };
            }).ToArray();

            return new BowlerOfTheYearPointsRaceSeriesDto
            {
                BowlerId = group.Key,
                BowlerName = group.First().BowlerName,
                Results = tournamentResults
            };
        })];
    }

    private static bool IsTournamentEligibleForRace(BoyProgressionResultDto result, BowlerOfTheYearCategory category)
    {
        if (category == BowlerOfTheYearCategory.Open
            || category == BowlerOfTheYearCategory.Youth
            || category == BowlerOfTheYearCategory.Rookie)
        {
            return result.StatsEligible;
        }

        if (category == BowlerOfTheYearCategory.Senior
            || category == BowlerOfTheYearCategory.SuperSenior)
        {
            return result.StatsEligible
                || result.TournamentType == TournamentType.Senior
                || result.TournamentType == TournamentType.SeniorAndWomen;
        }

        return category == BowlerOfTheYearCategory.Woman
        && (result.StatsEligible
            || result.TournamentType == TournamentType.Women
            || result.TournamentType == TournamentType.SeniorAndWomen
        );
    }

    private static bool IsBowlerEligibleForRace(BoyProgressionResultDto result, BowlerOfTheYearCategory category)
    {
        if (category == BowlerOfTheYearCategory.Open)
        {
            return true;
        }

        if (category == BowlerOfTheYearCategory.Woman)
        {
            return result.BowlerGender == Gender.Female;
        }

        if (category == BowlerOfTheYearCategory.Senior)
        {
            return result.BowlerDateOfBirth.HasValue && AgeAt(result.BowlerDateOfBirth.Value, result.TournamentEndDate) >= 50;
        }

        return category == BowlerOfTheYearCategory.SuperSenior
            ? result.BowlerDateOfBirth.HasValue && AgeAt(result.BowlerDateOfBirth.Value, result.TournamentEndDate) >= 60
            : category == BowlerOfTheYearCategory.Youth
                && result.BowlerDateOfBirth.HasValue 
                && AgeAt(result.BowlerDateOfBirth.Value, result.TournamentEndDate) < 18;
    }

    // Age completed on evaluationDate. Uses DateOnly.AddYears so Feb-29 birthdays are handled correctly.
    private static int AgeAt(DateOnly dateOfBirth, DateOnly evaluationDate)
    {
        var age = evaluationDate.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > evaluationDate)
        {
            age--;
        }

        return age;
    }

    private static int PointsForRace(BoyProgressionResultDto result, BowlerOfTheYearCategory category)
    {
        if (result.SideCutId is null)
        {
            return result.Points;
        }

        return DeriveSideCutBoyCategory(result.SideCutName) == category ? result.Points : 5;
    }

    private static BowlerOfTheYearCategory? DeriveSideCutBoyCategory(string? sideCutName) => sideCutName switch
    {
        "Senior" => BowlerOfTheYearCategory.Senior,
        "Super Senior" => BowlerOfTheYearCategory.SuperSenior,
        "Woman" or "Women" => BowlerOfTheYearCategory.Woman,
        _ => null
    };
}

internal interface IBowlerOfTheYearRaceCalculator
{
    IReadOnlyDictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> CalculateAllProgressions(IReadOnlyCollection<BoyProgressionResultDto> results);
}