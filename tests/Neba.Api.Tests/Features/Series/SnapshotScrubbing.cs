using System.Text.RegularExpressions;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Replaces ULIDs in a snapshot. Some Series factories mint IDs from the clock, so a seed alone does not fix them.</summary>
internal static partial class SnapshotScrubbing
{
    [GeneratedRegex("[0-9A-HJKMNP-TV-Z]{26}")]
    private static partial Regex UlidPattern();

    public static SettingsTask ScrubUlids(this SettingsTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return task.ScrubLinesWithReplace(line => UlidPattern().Replace(line, "<ulid>"));
    }
}
