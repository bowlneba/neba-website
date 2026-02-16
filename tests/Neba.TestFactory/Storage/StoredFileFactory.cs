using Neba.Application.Storage;

namespace Neba.TestFactory.Storage;

public static class StoredFileFactory
{
    public const string ValidContent = "This is a test file content.";
    public const string ValidContentType = "text/plain";
    public static readonly IReadOnlyDictionary<string, string> ValidMetadata = new Dictionary<string, string>
    {
        { "Author", "Test Author" },
        { "CreatedDate", DateTime.UtcNow.ToString("o") }
    };

    public static StoredFile Create(
        string? content = null,
        string? contentType = null,
        IDictionary<string, string>? metadata = null)
    {
        return new StoredFile
        {
            Content = content ?? ValidContent,
            ContentType = contentType ?? ValidContentType,
            Metadata = metadata ?? new Dictionary<string, string>(ValidMetadata)
        };
    }

    public static StoredFile Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<StoredFile> Bogus(int count, int? seed)
    {
        var faker = new Bogus.Faker<StoredFile>()
            .CustomInstantiator(f => Create(
                content: f.Lorem.Paragraph(),
                contentType: "text/plain",
                metadata: new Dictionary<string, string>
                {
                    { "Author", f.Person.FullName },
                    { "CreatedDate", f.Date.Past().ToString("o") }
                }));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}