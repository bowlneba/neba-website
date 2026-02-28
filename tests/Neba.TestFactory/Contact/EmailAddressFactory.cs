using Neba.Domain.Contact;

namespace Neba.TestFactory.Contact;

public static class EmailAddressFactory
{
    public const string ValidEmail = "test@domain.com";

    public static EmailAddress Create(string? email = null)
         => new()
         {
             Value = email ?? ValidEmail
         };

    public static EmailAddress Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<EmailAddress> Bogus(int count, int? seed = null)
    {
        var faker = new Bogus.Faker<EmailAddress>()
            .CustomInstantiator(f => Create(
                email: f.Internet.Email()));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}