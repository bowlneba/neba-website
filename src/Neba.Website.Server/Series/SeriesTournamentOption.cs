namespace Neba.Website.Server.Series;

/// <summary>A tournament the Stage grid can pick.</summary>
/// <param name="Id">The tournament ID.</param>
/// <param name="Label">The picker text, such as "Sat 7/12 · Name".</param>
/// <param name="StartDate">The tournament's first day.</param>
/// <param name="EndDate">The tournament's last day.</param>
/// <param name="TeamSize">Bowlers per team; 1 for singles.</param>
public sealed record SeriesTournamentOption(
    string Id, string Label, DateOnly StartDate, DateOnly EndDate, int TeamSize);
