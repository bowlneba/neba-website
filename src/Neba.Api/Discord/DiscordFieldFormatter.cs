using System.Globalization;
using System.Text;

namespace Neba.Api.Discord;

/// <summary>
/// Formats values for a Discord embed field, which allows 1024 characters.
/// </summary>
internal static class DiscordFieldFormatter
{
    public const int DefaultMaxIds = 20;

    public const int MaxFieldLength = 1024;

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

    /// <summary>
    /// Joins entries with newlines, dropping whole trailing entries (and noting how many) so the
    /// result stays within <paramref name="maxLength"/>.
    /// </summary>
    public static string JoinLines(IReadOnlyList<string> lines, int maxLength = MaxFieldLength)
    {
        if (lines.Count == 0)
        {
            return "None";
        }

        // Reserve room for the "+N more" suffix so it never pushes the value over the limit.
        const int suffixReserve = 24;
        var builder = new StringBuilder();
        var shown = 0;

        foreach (var line in lines)
        {
            var clipped = line.Length > maxLength - suffixReserve ? line[..(maxLength - suffixReserve)] : line;
            var added = builder.Length == 0 ? clipped.Length : clipped.Length + 1;

            if (builder.Length + added > maxLength - suffixReserve)
            {
                break;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(clipped);
            shown++;
        }

        return shown < lines.Count
            ? $"{builder}\n(+{lines.Count - shown} more)"
            : builder.ToString();
    }
}
