using Neba.Api.Documents;

namespace Neba.TestFactory.Documents;

public static class DocumentDtoFactory
{
    public const string ValidId = "12345abcde67890";
    public const string ValidName = "valid-document";
    public const string ValidContent = "<h1>Test Document</h1><p>This is a test document.</p>";
    public const string ValidContentType = "text/html";

    public static DocumentDto Create(
        string? id = null,
        string? name = null,
        string? content = null,
        string? contentType = null,
        DateTimeOffset? modifiedAt = null)
    {
        return new DocumentDto
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            Content = content ?? ValidContent,
            ContentType = contentType ?? ValidContentType,
            ModifiedAt = modifiedAt
        };
    }

    internal static IReadOnlyCollection<DocumentDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new DocumentDto
        {
            Id = faker.Random.AlphaNumeric(16),
            Name = faker.Lorem.Word(),
            Content = faker.Lorem.Paragraph(),
            ContentType = "text/plain"
        })];
    }

    public static IReadOnlyCollection<DocumentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}