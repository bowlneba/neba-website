using Bogus;

using Neba.Domain;
using Neba.Domain.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class SideCutCriteriaGroupFactory
{
    public static readonly LogicalOperator ValidLogicalOperator = LogicalOperator.And;
    public const int ValidSortOrder = 1;

    public static SideCutCriteriaGroup Create(
        SideCut? sideCut = null,
        LogicalOperator? logicalOperator = null,
        int? sortOrder = null)
    {
        var resolvedSideCut = sideCut ?? SideCutFactory.Create();
        return new()
        {
            SideCutId = resolvedSideCut.Id,
            SideCut = resolvedSideCut,
            LogicalOperator = logicalOperator ?? ValidLogicalOperator,
            SortOrder = sortOrder ?? ValidSortOrder,
        };
    }

    public static IReadOnlyCollection<SideCutCriteriaGroup> Bogus(
        int count,
        IReadOnlyCollection<SideCut>? sideCutPool = null,
        int? seed = null)
    {
        var usedSortOrders = new Dictionary<SideCutId, HashSet<int>>();
        sideCutPool ??= SideCutFactory.Bogus(10, seed);

        var faker = new Faker<SideCutCriteriaGroup>()
            .CustomInstantiator(f =>
            {
                var sideCut = f.PickRandom(sideCutPool.ToArray());

                if (!usedSortOrders.TryGetValue(sideCut.Id, out var used))
                {
                    used = [];
                    usedSortOrders[sideCut.Id] = used;
                }

                int sortOrder;
                do { sortOrder = f.Random.Int(1, 1000); } while (!used.Add(sortOrder));

                return new SideCutCriteriaGroup
                {
                    SideCutId = sideCut.Id,
                    SideCut = sideCut,
                    LogicalOperator = f.PickRandom(new[] { LogicalOperator.And, LogicalOperator.Or }.ToArray()),
                    SortOrder = sortOrder,
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
