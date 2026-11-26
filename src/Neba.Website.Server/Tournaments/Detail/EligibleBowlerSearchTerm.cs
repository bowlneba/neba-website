namespace Neba.Website.Server.Tournaments.Detail;

// A typed search term, prepared once per search rather than once per bowler.
internal sealed record EligibleBowlerSearchTerm
{
    // Empty for a blank term, which every bowler matches.
    public required IReadOnlyList<string> Words { get; init; }

    // Null when the term has no digit, so it is not tried against USBC IDs.
    public string? UsbcKey { get; init; }

    public static EligibleBowlerSearchTerm From(string term)
        => new()
        {
            Words = EligibleBowlerViewModel.NormalizeName(term)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries),
            UsbcKey = term.Any(char.IsAsciiDigit)
                ? EligibleBowlerViewModel.NormalizeUsbcId(term)
                : null
        };
}