using Neba.Api.Email;

namespace Neba.TestFactory.Email;

public static class EmailMessageFactory
{
    public const string ValidTo = "test@example.com";
    public const string ValidSubject = "Test Subject";
    public const string ValidHtmlBody = "<p>Test body</p>";

    public static EmailMessage Create(
        string? to = null,
        string? subject = null,
        string? htmlBody = null,
        string? replyTo = null)
        => new()
        {
            To = to ?? ValidTo,
            Subject = subject ?? ValidSubject,
            HtmlBody = htmlBody ?? ValidHtmlBody,
            ReplyTo = replyTo,
        };

    public static IReadOnlyCollection<EmailMessage> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new EmailMessage
        {
            To = faker.Internet.Email(),
            Subject = faker.Random.Words(3),
            HtmlBody = $"<p>{faker.Lorem.Paragraph()}</p>",
            ReplyTo = faker.Random.Bool() ? faker.Internet.Email() : null,
        })];
    }

    public static IReadOnlyCollection<EmailMessage> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}