using Neba.Application.Documents.GetDocument;

namespace Neba.TestFactory.Documents;

public static class GetDocumentDtoFactory
{
    public const string ValidHtml = "<h1>Test Document</h1><p>This is a test document.</p>";
    public static readonly DateTimeOffset ValidCachedAt = new(2026, 1, 1, 5, 0, 0, TimeSpan.Zero);

    public static GetDocumentDto Create(
        string? html = null,
        DateTimeOffset? lastedUpdatedAt = null)
    {
        return new GetDocumentDto
        {
            Html = html ?? ValidHtml,
            LastUpdated = lastedUpdatedAt ?? ValidCachedAt
        };
    }

    public static GetDocumentDto Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<GetDocumentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Bogus.Faker<GetDocumentDto>()
            .CustomInstantiator(f => new GetDocumentDto
            {
                Html = $"<h1>{f.Lorem.Word()}</h1><p>{f.Lorem.Paragraph()}</p>",
                LastUpdated = new DateTimeOffset(f.Date.Past(), TimeSpan.Zero)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}