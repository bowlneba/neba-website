namespace Neba.Website.Server.History.Champions;

internal static class ChampionsHelpers
{
    public static string MonthAbbreviation(int month) => month switch
    {
        1 => "Jan",
        2 => "Feb",
        3 => "Mar",
        4 => "Apr",
        5 => "May",
        6 => "Jun",
        7 => "Jul",
        8 => "Aug",
        9 => "Sep",
        10 => "Oct",
        11 => "Nov",
        12 => "Dec",
        _ => string.Empty
    };

    public static string TypePillClass(string tournamentType)
    {
        if (tournamentType.Contains("double", StringComparison.OrdinalIgnoreCase)) return "type-doubles";
        if (tournamentType.Contains("trio", StringComparison.OrdinalIgnoreCase)) return "type-trios";
        if (tournamentType.Contains("team", StringComparison.OrdinalIgnoreCase)) return "type-team";
        if (tournamentType.Contains("senior", StringComparison.OrdinalIgnoreCase)) return "type-senior";
        if (tournamentType.Contains("women", StringComparison.OrdinalIgnoreCase)) return "type-women";
        if (tournamentType.Contains("special", StringComparison.OrdinalIgnoreCase)) return "type-special";
        return "type-singles";
    }
}