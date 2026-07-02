using Neba.Api.Contacts.Domain;

namespace Neba.TestFactory.Contact;

public static class EmailAddressFactory
{
    public const string ValidEmail = "test@domain.com";

    public static EmailAddress Create(string? email = null)
         => new()
         {
             Value = email ?? ValidEmail
         };

    internal static IReadOnlyCollection<EmailAddress> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => Create(email: faker.Internet.Email()))];
    }

    public static IReadOnlyCollection<EmailAddress> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}