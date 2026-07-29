using Bogus;

using Neba.Api.Contracts.Security.CreateUser;

namespace Neba.TestFactory.Security;

public static class ClaimInputFactory
{
    public const string ValidType = "permission";
    public const string ValidValue = "Tournaments.CreateTournament";

    public static ClaimInput Create(
        string? type = null,
        string? value = null)
        => new()
        {
            Type = type ?? ValidType,
            Value = value ?? ValidValue,
        };

    internal static IReadOnlyCollection<ClaimInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new ClaimInput
        {
            Type = faker.Random.Word(),
            Value = faker.Random.Word(),
        })];
    }

    public static IReadOnlyCollection<ClaimInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}