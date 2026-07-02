using System.Net.Mime;

using Neba.Api.Contracts.News.GetArticle;

namespace Neba.TestFactory.News;

public static class ArticleAttachmentResponseFactory
{
    public const string ValidDisplayName = "NEBA Rulebook";

    public static ArticleAttachmentResponse Create(
        string? displayName = null,
        Uri? url = null,
        string? contentType = null)
        => new()
        {
            DisplayName = displayName ?? ValidDisplayName,
            Url = url,
            ContentType = contentType ?? MediaTypeNames.Image.Jpeg,
        };

    internal static IReadOnlyCollection<ArticleAttachmentResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new ArticleAttachmentResponse
        {
            DisplayName = faker.Random.Words(2),
            Url = faker.Random.Bool() ? new Uri(faker.Internet.Url()) : null,
            ContentType = faker.System.MimeType()
        })];
    }

    public static IReadOnlyCollection<ArticleAttachmentResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}