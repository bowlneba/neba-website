using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Neba.Api.Contracts.Slugs;

/// <summary>
/// Normalizes a title or a staff-supplied slug override into a URL-safe slug: lowercase,
/// alphanumeric runs joined by single hyphens, no leading/trailing hyphen.
/// </summary>
public static class SlugNormalizer
{
    /// <summary>
    /// Normalizes <paramref name="value"/> into a URL-safe slug.
    /// </summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Slugs are URL-facing and must be lowercase, not normalized for security comparisons.")]
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var lowered = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var lastWasHyphen = false;

        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().TrimEnd('-');
    }
}
