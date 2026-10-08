using System.Globalization;

namespace Neba.Website.Server;

/// <summary>
/// Culture used for Humanizer output (ordinals, plurals); the site is English-only.
/// </summary>
internal static class DisplayCulture
{
    public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
}
