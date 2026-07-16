using System.Text;

namespace Neba.Website.Server.Services;

/// <summary>
/// Client-side preview mirroring <c>Neba.Api.Domain.SlugNormalizer.Normalize</c> (lowercase, alphanumeric
/// runs joined by single hyphens, no leading/trailing hyphen). Cosmetic only — the actual slug is always
/// computed server-side from whichever value is submitted (this preview, or a staff-typed override).
/// </summary>
internal static class SlugPreviewGenerator
{
    public static string Generate(string value)
    {
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
