using System.Drawing;
using System.Globalization;

using Bogus.DataSets;

using Neba.Domain;
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class SideCutFactory
{
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
            .CustomInstantiator(f => new SideCut
            {
                Id = SideCutId.Parse(Ulid.BogusString(f), CultureInfo.InvariantCulture),
                Name = f.Lorem.Word(),
                Indicator = ColorTranslator.FromHtml(f.Internet.Color(format: ColorFormat.Hex)),
                LogicalOperator = f.PickRandom(LogicalOperator.List.ToArray()),
                Active = f.Random.Bool()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}