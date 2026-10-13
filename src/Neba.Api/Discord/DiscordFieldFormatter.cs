using System.Globalization;

namespace Neba.Api.Discord;

/// <summary>
/// Formats values for a Discord embed field, which allows 1024 characters.
/// </summary>
internal static class DiscordFieldFormatter
{
    public const int DefaultMaxIds = 20;

    public static string JoinIds(IEnumerable<int> ids, int max = DefaultMaxIds)
    {
        var ordered = ids.Distinct().Order().ToList();

        if (ordered.Count == 0)
        {
            return "None";
        }

        var shown = string.Join(", ", ordered.Take(max).Select(id => id.ToString(CultureInfo.InvariantCulture)));

        return ordered.Count > max
            ? $"{shown} (+{ordered.Count - max} more)"
            : shown;
    }
}
