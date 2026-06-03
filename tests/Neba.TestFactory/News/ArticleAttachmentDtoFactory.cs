using Bogus;

using Neba.Api.Features.News.GetArticle;

namespace Neba.TestFactory.News;

public static class ArticleAttachmentDtoFactory
{
    public const string ValidDisplayName = "NEBA Rulebook";
    public static readonly Uri ValidUrl = new("https://storage.example.com/documents/attachments/rulebook.pdf");

    public static ArticleAttachmentDto Create(
        string? displayName = null,
        Uri? url = null)
        => new()
        {
            DisplayName = displayName ?? ValidDisplayName,
            Url = url ?? ValidUrl,
        };

    public static IReadOnlyCollection<ArticleAttachmentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleAttachmentDto>()
            .CustomInstantiator(f => new ArticleAttachmentDto
            {
                DisplayName = f.Random.Words(2),
                Url = new Uri(f.Internet.Url()),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
