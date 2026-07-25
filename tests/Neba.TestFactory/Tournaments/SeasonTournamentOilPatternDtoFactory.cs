using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;

namespace Neba.TestFactory.Tournaments;

public static class SeasonTournamentOilPatternDtoFactory
{
    public const string ValidName = "Test Pattern";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 22.5m;
    public const decimal ValidLeftRatio = 4.0m;
    public const decimal ValidRightRatio = 6.0m;

    public static SeasonTournamentOilPatternDto Create(
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null,
        IReadOnlyCollection<string>? tournamentRounds = null)
        => new()
        {
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Volume = volume ?? ValidVolume,
            LeftRatio = leftRatio ?? ValidLeftRatio,
            RightRatio = rightRatio ?? ValidRightRatio,
            KegelId = kegelId,
            TournamentRounds = tournamentRounds ?? [TournamentRound.Qualifying.Name],
        };

    public static IReadOnlyCollection<SeasonTournamentOilPatternDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonTournamentOilPatternDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Lorem.Word() + " Pattern",
                Length = f.Random.Int(32, 47),
                Volume = f.Random.Decimal(15, 35),
                LeftRatio = f.Random.Decimal(2, 8),
                RightRatio = f.Random.Decimal(2, 8),
                KegelId = f.Random.Bool() ? f.Random.Guid() : null,
                TournamentRounds = [.. f.PickRandom(TournamentRound.List.ToArray(), f.Random.Int(1, TournamentRound.List.Count)).Select(r => r.Name)],
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<SeasonTournamentOilPatternDto> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}