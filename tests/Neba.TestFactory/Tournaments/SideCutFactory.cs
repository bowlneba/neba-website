using System.Drawing;
using System.Globalization;

using Bogus.DataSets;

using ErrorOr;

using Neba.Domain;
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class SideCutFactory
{
    private static void EnsureSuccess(ErrorOr<Success> result)
    {
        if (!result.IsError)
        {
            return;
        }

        throw new InvalidOperationException(result.FirstError.Description);
    }

    public static SideCut Create(
        SideCutId? id = null,
        string? name = null,
        Color? indicator = null,
        LogicalOperator? logicalOperator = null,
        bool active = true,
        IReadOnlyCollection<SideCutCriteriaGroup>? criteriaGroups = null)
    {
        var sideCut = new SideCut
        {
            Id = id ?? SideCutId.New(),
            Name = name ?? "Test Side Cut",
            LogicalOperator = logicalOperator ?? LogicalOperator.And,
            Indicator = indicator ?? Color.Red,
            Active = active
        };

        if (criteriaGroups is not null)
        {
            foreach (var criteriaGroup in criteriaGroups.OrderBy(group => group.SortOrder))
            {
                var groupId = sideCut.AddCriteriaGroup(criteriaGroup.LogicalOperator, criteriaGroup.SortOrder).Value;

                foreach (var criterion in criteriaGroup.Criteria)
                {
                    if (criterion.GenderRequirement is not null)
                    {
                        EnsureSuccess(sideCut.AddCriteria(groupId, criterion.GenderRequirement));
                        continue;
                    }

                    EnsureSuccess(sideCut.AddCriteria(groupId, criterion.MinimumAge, criterion.MaximumAge));
                }
            }
        }

        return sideCut;
    }

    public static SideCut Senior()
    {
        var sideCut = new SideCut
        {
            Id = SideCutId.New(),
            Name = "Senior",
            LogicalOperator = LogicalOperator.And,
            Indicator = Color.Blue,
            Active = true
        };

        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;
        sideCut.AddCriteria(groupId, minimumAge: 50, maximumAge: null);

        return sideCut;
    }

    public static SideCut SuperSenior()
    {
        var sideCut = new SideCut
        {
            Id = SideCutId.New(),
            Name = "Super Senior",
            LogicalOperator = LogicalOperator.And,
            Indicator = Color.Green,
            Active = true
        };

        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;
        sideCut.AddCriteria(groupId, minimumAge: 60, maximumAge: null);

        return sideCut;
    }

    public static SideCut Women()
    {
        var sideCut = new SideCut
        {
            Id = SideCutId.New(),
            Name = "Women",
            LogicalOperator = LogicalOperator.And,
            Indicator = Color.Pink,
            Active = true
        };

        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;
        sideCut.AddCriteria(groupId, Gender.Female);

        return sideCut;
    }

    public static IReadOnlyCollection<SideCut> StandardNebaSideCuts() =>
        [Senior(), SuperSenior(), Women()];

    public static IReadOnlyCollection<SideCut> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SideCut>()
            .CustomInstantiator(f =>
            {
                var sideCut = new SideCut
                {
                    Id = SideCutId.Parse(Ulid.BogusString(f), CultureInfo.InvariantCulture),
                    Name = f.Lorem.Word(),
                    Indicator = ColorTranslator.FromHtml(f.Internet.Color(format: ColorFormat.Hex)),
                    LogicalOperator = f.PickRandom(LogicalOperator.And, LogicalOperator.Or),
                    Active = f.Random.Bool()
                };

                var groupCount = f.Random.Int(1, 3);
                var usedSortOrders = new HashSet<int>();

                for (var i = 0; i < groupCount; i++)
                {
                    int sortOrder;
                    do { sortOrder = f.Random.Int(1, 20); } while (!usedSortOrders.Add(sortOrder));

                    var groupId = sideCut.AddCriteriaGroup(
                        f.PickRandom(LogicalOperator.And, LogicalOperator.Or),
                        sortOrder).Value;

                    if (f.Random.Bool())
                    {
                        sideCut.AddCriteria(groupId, f.Random.Int(1, 65), null);
                    }
                    else
                    {
                        sideCut.AddCriteria(groupId, f.PickRandom(Gender.List.ToArray()));
                    }
                }

                return sideCut;
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}