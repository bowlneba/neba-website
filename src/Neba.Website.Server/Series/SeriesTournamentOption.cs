namespace Neba.Website.Server.Series;

internal sealed record SeriesTournamentOption(
    string Id, string Label, DateOnly StartDate, DateOnly EndDate, int TeamSize);
