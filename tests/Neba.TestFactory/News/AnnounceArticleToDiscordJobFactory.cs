using Neba.Api.Features.News.AnnounceArticleToDiscord;
using Neba.Api.Features.News.Domain;

namespace Neba.TestFactory.News;

public static class AnnounceArticleToDiscordJobFactory
{
    public static readonly DateTimeOffset ValidExpectedPublishDateUtc = ArticleFactory.ValidPublishDateUtc;

    public static AnnounceArticleToDiscordJob Create(
        ArticleId? articleId = null,
        DateTimeOffset? expectedPublishDateUtc = null)
        => new()
        {
            ArticleId = articleId ?? ArticleId.New(),
            ExpectedPublishDateUtc = expectedPublishDateUtc ?? ValidExpectedPublishDateUtc
        };

    public static IReadOnlyCollection<AnnounceArticleToDiscordJob> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => new AnnounceArticleToDiscordJob
        {
            ArticleId = new ArticleId(Ulid.BogusString(faker)),
            ExpectedPublishDateUtc = faker.Date.PastOffset(2).ToUniversalTime()
        })];
    }
}
