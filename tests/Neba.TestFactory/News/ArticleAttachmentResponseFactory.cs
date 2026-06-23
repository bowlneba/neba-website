using System.Net.Mime;

using Bogus;

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

    public static IReadOnlyCollection<ArticleAttachmentResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleAttachmentResponse>()
            .CustomInstantiator(f => new ArticleAttachmentResponse
            {
                DisplayName = f.Random.Words(2),
                Url = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                ContentType = f.System.MimeType()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ArticleAttachmentResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}