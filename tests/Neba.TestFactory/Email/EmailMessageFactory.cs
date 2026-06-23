using Bogus;

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

    public static IReadOnlyCollection<EmailMessage> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<EmailMessage>()
            .CustomInstantiator(f => new EmailMessage
            {
                To = f.Internet.Email(),
                Subject = f.Random.Words(3),
                HtmlBody = $"<p>{f.Lorem.Paragraph()}</p>",
                ReplyTo = f.Random.Bool() ? f.Internet.Email() : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<EmailMessage> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
