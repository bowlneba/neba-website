namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// The Hall of Fame category names as the API sends them, with their display order and color class.
/// </summary>
internal static class HallOfFameCategories
{
    /// <summary>
    /// The category names in display order.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = ["Superior Performance", "Meritorious Service", "Friend of NEBA"];

    /// <summary>
    /// Position of a category in the display order; unknown names sort last.
    /// </summary>
    public static int OrderOf(string category)
    {
        var index = Names.ToList().IndexOf(category);
        return index < 0 ? Names.Count : index;
    }

    /// <summary>
    /// The CSS class that colors the dot beside a category; empty for an unknown name.
    /// </summary>
    public static string DotClass(string category) => category switch
    {
        "Superior Performance" => "hof-dot--superior",
        "Meritorious Service" => "hof-dot--meritorious",
        "Friend of NEBA" => "hof-dot--friend",
        _ => string.Empty
    };
}
