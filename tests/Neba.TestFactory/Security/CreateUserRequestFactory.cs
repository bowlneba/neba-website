using Bogus;

using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Security.Domain;

namespace Neba.TestFactory.Security;

public static class CreateUserRequestFactory
{
    public const string ValidEmail = "newstaff@bowlneba.com";

    public static CreateUserRequest Create(
        string? email = null,
        IReadOnlyCollection<string>? roles = null,
        string? usbcId = null,
        string? phoneNumber = null,
        IReadOnlyCollection<ClaimInput>? claims = null)
        => new()
        {
            User = new CreateUserInput
            {
                Email = email ?? ValidEmail,
                Roles = roles ?? [Roles.Webmaster],
                UsbcId = usbcId,
                PhoneNumber = phoneNumber,
                Claims = claims ?? []
            }
        };

    internal static IReadOnlyCollection<CreateUserRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var eligibleRoles = Roles.All.Where(r => r != Roles.Admin).ToArray();
        return [.. Enumerable.Range(0, count).Select(_ => new CreateUserRequest
        {
            User = new CreateUserInput
            {
                Email = faker.Internet.Email(),
                Roles = [faker.PickRandom(eligibleRoles)],
                UsbcId = faker.Random.Bool() ? faker.Random.AlphaNumeric(8) : null,
                PhoneNumber = faker.Random.Bool() ? faker.Phone.PhoneNumber() : null
            }
        })];
    }

    public static IReadOnlyCollection<CreateUserRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
