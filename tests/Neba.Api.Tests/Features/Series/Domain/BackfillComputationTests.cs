using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class BackfillComputationTests
{
    private static IReadOnlyList<FieldEntryDraft> Compute(
        int vacancies,
        IReadOnlyList<BowlerPointsFact> points,
        IEnumerable<BowlerId>? inField = null,
        IEnumerable<BowlerId>? barred = null,
        IEnumerable<BowlerId>? members = null)
        => BackfillComputation.Compute(
            vacancies,
            (inField ?? []).ToHashSet(),
            (barred ?? []).ToHashSet(),
            (members ?? points.Select(p => p.BowlerId)).ToHashSet(),
            points);

    private static BowlerPointsFact Points(BowlerId id, int points) => BowlerPointsFactFactory.Create(id, points);

    [Theory(DisplayName = "Compute should return nothing when there are no vacancies")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Compute_ShouldReturnNothing_WhenNoVacancies(int vacancies)
    {
        // Arrange
        var ids = AdvancementTestData.Ids(1);

        // Act
        var result = Compute(vacancies, [Points(ids[0], 100)]);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Compute should fill vacancies from the highest points with Backfill entries")]
    public void Compute_ShouldTakeHighestPoints()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(2, [Points(ids[0], 50), Points(ids[1], 90), Points(ids[2], 70)]);

        // Assert
        result.ShouldBe([
            new FieldEntryDraft(ids[1], null, null, 90, false),
            new FieldEntryDraft(ids[2], null, null, 70, false)]);
    }

    [Fact(DisplayName = "Compute should advance every bowler tied at the cutoff")]
    public void Compute_ShouldAdvanceAllTiedAtCutoff()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(4);

        // Act
        var result = Compute(2, [Points(ids[0], 100), Points(ids[1], 90), Points(ids[2], 90), Points(ids[3], 80)]);

        // Assert
        result.Select(e => e.BowlerId).ShouldBe([ids[0], ids[1], ids[2]]);
    }

    [Fact(DisplayName = "Compute should advance every candidate when there are fewer candidates than vacancies")]
    public void Compute_ShouldAdvanceAll_WhenFewerCandidatesThanVacancies()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(5, [Points(ids[0], 60), Points(ids[1], 40)]);

        // Assert
        result.Select(e => e.BowlerId).ShouldBe([ids[0], ids[1]]);
    }

    [Fact(DisplayName = "Compute should skip a bowler with zero points")]
    public void Compute_ShouldSkipZeroPoints()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(3, [Points(ids[0], 10), Points(ids[1], 0), Points(ids[2], 0)]);

        // Assert
        result.Select(e => e.BowlerId).ShouldBe([ids[0]]);
    }

    [Fact(DisplayName = "Compute should return nothing when every bowler has zero points")]
    public void Compute_ShouldReturnNothing_WhenAllZeroPoints()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(2, [Points(ids[0], 0), Points(ids[1], 0)]);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Compute should skip a bowler already in the field")]
    public void Compute_ShouldSkipInField()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(1, [Points(ids[0], 100), Points(ids[1], 50)], inField: [ids[0]]);

        // Assert
        result.Select(e => e.BowlerId).ShouldBe([ids[1]]);
    }

    [Fact(DisplayName = "Compute should skip a bowler barred by a Clear override")]
    public void Compute_ShouldSkipBarred()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(1, [Points(ids[0], 100), Points(ids[1], 50)], barred: [ids[0]]);

        // Assert
        result.Select(e => e.BowlerId).ShouldBe([ids[1]]);
    }

    [Fact(DisplayName = "Compute should skip a bowler who is not a member on the as-of date")]
    public void Compute_ShouldSkipNonMember()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(2);

        // Act
        var result = Compute(1, [Points(ids[0], 100), Points(ids[1], 50)], members: [ids[1]]);

        // Assert
        result.Select(e => e.BowlerId).ShouldBe([ids[1]]);
    }

    [Fact(DisplayName = "Compute should return nothing when every candidate is excluded")]
    public void Compute_ShouldReturnNothing_WhenNoEligibleCandidates()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(3);

        // Act
        var result = Compute(
            2,
            [Points(ids[0], 100), Points(ids[1], 90), Points(ids[2], 80)],
            inField: [ids[0]],
            barred: [ids[1]],
            members: [ids[0], ids[1]]);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Compute should break equal points by BowlerId and return equal output for reversed input")]
    public void Compute_ShouldBeDeterministic()
    {
        // Arrange
        var ids = AdvancementTestData.Ids(5);
        IReadOnlyList<BowlerPointsFact> points = [Points(ids[0], 70), Points(ids[1], 90), Points(ids[2], 90), Points(ids[3], 90), Points(ids[4], 10)];

        // Act
        var first = Compute(2, points);
        var second = Compute(2, points);
        var reversed = Compute(2, [.. points.Reverse()]);

        // Assert
        first.Select(e => e.BowlerId).ShouldBe([ids[1], ids[2], ids[3]]);
        second.ShouldBe(first);
        reversed.ShouldBe(first);
    }
}