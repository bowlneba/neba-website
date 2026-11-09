using Neba.Api.Discord;

namespace Neba.TestFactory.Discord;

internal static class NewsAnnouncementFactory
{
    public const string ValidTitle = "Test Article";
    public static readonly Uri ValidArticleUrl = new("https://bowlneba.com/news/test-article");

    public static NewsAnnouncement Create(
        string? title = null,
        Uri? articleUrl = null,
        Uri? imageUrl = null)
        => new(
            title ?? ValidTitle,
            articleUrl ?? ValidArticleUrl,
            imageUrl);

    internal static IReadOnlyCollection<NewsAnnouncement> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new NewsAnnouncement(
            faker.Lorem.Sentence(4),
            new Uri($"https://bowlneba.com/news/{faker.Lorem.Slug()}"),
            faker.Random.Bool() ? new Uri(faker.Internet.Avatar()) : null))];
    }

    internal static IReadOnlyCollection<NewsAnnouncement> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}