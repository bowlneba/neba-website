using Neba.Api.Features.Documents.GetDocument;

namespace Neba.TestFactory.Documents;

internal static class GetDocumentDtoFactory
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

    public static IReadOnlyCollection<GetDocumentDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new GetDocumentDto
        {
            Html = $"<h1>{faker.Lorem.Word()}</h1><p>{faker.Lorem.Paragraph()}</p>",
            LastUpdated = new DateTimeOffset(faker.Date.Past(), TimeSpan.Zero)
        })];
    }

    public static IReadOnlyCollection<GetDocumentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}