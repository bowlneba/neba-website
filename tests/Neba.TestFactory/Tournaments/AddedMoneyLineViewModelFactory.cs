using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class AddedMoneyLineViewModelFactory
{
    public const string ValidName = "Acme Corp";
    public const decimal ValidAmount = 500m;

    public static AddedMoneyLineViewModel Create(string? name = null, decimal? amount = null, bool? isNeba = null)
        => new()
        {
            Name = name ?? ValidName,
            Amount = amount ?? ValidAmount,
            IsNeba = isNeba ?? false,
        };

    public static IReadOnlyCollection<AddedMoneyLineViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<AddedMoneyLineViewModel>()
            .CustomInstantiator(f => new AddedMoneyLineViewModel
            {
                Name = f.Company.CompanyName(),
                Amount = f.Random.Decimal(100, 5000),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
