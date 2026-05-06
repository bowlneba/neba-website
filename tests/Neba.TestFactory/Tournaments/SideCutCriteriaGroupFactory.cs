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
        int? sortOrder = null,
        SideCutCriteriaGroupId? id = null,
        IReadOnlyCollection<SideCutCriteria>? criteria = null)
    {
        var resolvedSideCut = sideCut ?? SideCutFactory.Create();
        var group = new SideCutCriteriaGroup
        {
            Id = id ?? SideCutCriteriaGroupId.New(),
            SideCut = resolvedSideCut,
            LogicalOperator = logicalOperator ?? ValidLogicalOperator,
            SortOrder = sortOrder ?? ValidSortOrder,
        };

        if (criteria is not null)
        {
            foreach (var criterion in criteria)
            {
                if (criterion.GenderRequirement is not null)
                    group.AddCriteria(criterion.GenderRequirement);
                else
                    group.AddCriteria(criterion.MinimumAge, criterion.MaximumAge);
            }
        }

        return group;
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

                var group = new SideCutCriteriaGroup
                {
                    Id = SideCutCriteriaGroupId.New(),
                    SideCut = sideCut,
                    LogicalOperator = f.PickRandom(new[] { LogicalOperator.And, LogicalOperator.Or }.ToArray()),
                    SortOrder = sortOrder,
                };

                foreach (var criterion in SideCutCriteriaFactory.Bogus(f.Random.Int(1, 3)))
                {
                    if (criterion.GenderRequirement is not null)
                        group.AddCriteria(criterion.GenderRequirement);
                    else
                        group.AddCriteria(criterion.MinimumAge, criterion.MaximumAge);
                }

                return group;
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}