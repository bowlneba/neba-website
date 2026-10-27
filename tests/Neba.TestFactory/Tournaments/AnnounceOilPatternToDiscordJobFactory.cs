using Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

internal static class AnnounceOilPatternToDiscordJobFactory
{
    public static readonly DateTimeOffset ValidExpectedRevealDateTime = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    public static AnnounceOilPatternToDiscordJob Create(
        TournamentId? tournamentId = null,
        DateTimeOffset? expectedRevealDateTime = null)
        => new()
        {
            TournamentId = tournamentId ?? TournamentId.New(),
            ExpectedRevealDateTime = expectedRevealDateTime ?? ValidExpectedRevealDateTime
        };

    public static IReadOnlyCollection<AnnounceOilPatternToDiscordJob> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => new AnnounceOilPatternToDiscordJob
        {
            TournamentId = new TournamentId(Ulid.BogusString(faker)),
            ExpectedRevealDateTime = faker.Date.FutureOffset(2).ToUniversalTime()
        })];
    }
}