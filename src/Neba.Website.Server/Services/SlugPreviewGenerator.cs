using Neba.Api.Contracts.Slugs;

namespace Neba.Website.Server.Services;

/// <summary>
/// Client-side preview mirroring <see cref="SlugNormalizer.Normalize"/>. Cosmetic only — the actual
/// slug is always computed server-side from whichever value is submitted (this preview, or a
/// staff-typed override).
/// </summary>
internal static class SlugPreviewGenerator
{
    public static string Generate(string value) => SlugNormalizer.Normalize(value);
}
