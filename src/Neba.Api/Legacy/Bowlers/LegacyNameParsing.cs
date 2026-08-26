namespace Neba.Api.Legacy.Bowlers;

// Shared by NewBowlerSyncJob and UpdateBowlerSyncJob: the Software stores FirstName as one plain
// free-text field with no nickname concept of its own (confirmed - no quote/nickname parsing exists
// anywhere in nebamgmt-v3). A bowler entered as `William "Bill"` in the Software's FirstName field is
// split here into FirstName "William" / Nickname "Bill" before either sync job maps it into Name.Create.
internal static class LegacyNameParsing
{
    extension(string firstName)
    {
        public (string FirstName, string? Nickname) ExtractQuotedNickname()
        {
            var firstQuote = firstName.IndexOf('"', StringComparison.CurrentCulture);
            if (firstQuote < 0)
            {
                return (firstName.Trim(), null);
            }

            var secondQuote = firstName.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
            {
                // Unbalanced quote - treat the whole field as the first name rather than guessing.
                return (firstName.Trim(), null);
            }

            var nickname = firstName[(firstQuote + 1)..secondQuote].Trim();

            var remainder = string.Concat(
                firstName.AsSpan(0, firstQuote),
                firstName.AsSpan(secondQuote + 1)).Trim();

            return (remainder, string.IsNullOrWhiteSpace(nickname) ? null : nickname);
        }
    }
}