using Neba.Api.Discord;

namespace Neba.TestFactory.Discord;

internal static class DiscordAlertFactory
{
    public const string ValidTitle = "Test Alert";
    public const string ValidBody = "Something needs attention.";

    public static DiscordAlert Create(
        DiscordAlertSeverity? severity = null,
        string? title = null,
        string? body = null,
        IReadOnlyDictionary<string, string>? metadata = null)
        => new(
            severity ?? DiscordAlertSeverity.Warning,
            title ?? ValidTitle,
            body ?? ValidBody,
            metadata);

    internal static IReadOnlyCollection<DiscordAlert> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var severities = DiscordAlertSeverity.List.ToList();

        return [.. Enumerable.Range(0, count).Select(_ => new DiscordAlert(
            faker.Random.ListItem(severities),
            faker.Lorem.Sentence(3),
            faker.Lorem.Sentence(8),
            null))];
    }

    public static IReadOnlyCollection<DiscordAlert> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}