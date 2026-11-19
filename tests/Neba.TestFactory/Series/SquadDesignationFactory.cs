using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class SquadDesignationFactory
{
    public static readonly DateOnly ValidDate = new(2025, 7, 12);
    public static readonly TimeOnly ValidStartTime = new(9, 0);

    public static SquadDesignation Create(DateOnly? date = null, TimeOnly? startTime = null)
        => SquadDesignation.Describe(date ?? ValidDate, startTime ?? ValidStartTime);
}