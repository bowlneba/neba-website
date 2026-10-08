using System.Globalization;

using Humanizer;

namespace Neba.Website.Server;

/// <summary>
/// Culture used for Humanizer output (ordinals, plurals); the site is English-only.
/// </summary>
internal static class DisplayCulture
{
    public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
}

/// <summary>
/// Display formatting for rank and place numbers.
/// </summary>
internal static class OrdinalExtensions
{
    extension(int number)
    {
        /// <summary>
        /// Formats the number as an English ordinal ("1st", "22nd").
        /// </summary>
        public string ToOrdinal() => number.Ordinalize(DisplayCulture.English);
    }
}
