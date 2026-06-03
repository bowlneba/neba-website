using Bogus;

using Neba.Api.Features.News.GetArticle;

namespace Neba.TestFactory.News;

public static class ArticleAttachmentDtoFactory
{
    public const string ValidDisplayName = "NEBA Rulebook";
    public const string ValidContainer = "documents";
    public const string ValidPath = "attachments/rulebook.pdf";

    public static ArticleAttachmentDto Create(
        string? displayName = null,
        string? container = null,
        string? path = null,
        Uri? url = null)
        => new()
        {
            DisplayName = displayName ?? ValidDisplayName,
            Container = container ?? ValidContainer,
            Path = path ?? ValidPath,
            Url = url,
        };

    public static IReadOnlyCollection<ArticleAttachmentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleAttachmentDto>()
            .CustomInstantiator(f => new ArticleAttachmentDto
            {
                DisplayName = f.Random.Words(2),
                Container = f.Random.Word(),
                Path = $"attachments/{f.System.FileName()}",
                Url = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
