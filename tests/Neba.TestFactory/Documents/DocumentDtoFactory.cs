using Neba.Application.Documents;

namespace Neba.TestFactory.Documents;

public static class DocumentDtoFactory
{
    public const string ValidName = "valid-document";
    public const string ValidContent = "<h1>Test Document</h1><p>This is a test document.</p>";
    public const string ValidContentType = "text/html";

    public static DocumentDto Create(
        string? name = null,
        string? content = null,
        string? contentType = null)
    {
        return new DocumentDto
        {
            Name = name ?? ValidName,
            Content = content ?? ValidContent,
            ContentType = contentType ?? ValidContentType
        };
    }

    public static DocumentDto Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<DocumentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Bogus.Faker<DocumentDto>()
            .CustomInstantiator(f => new DocumentDto
            {
                Name = f.Lorem.Word(),
                Content = f.Lorem.Paragraph(),
                ContentType = "text/plain"
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}