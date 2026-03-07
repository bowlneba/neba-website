namespace Neba.Website.Server.Contact;

internal static class PostalCodeFormatter
{
    public static string FormatForDisplay(string postalCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);

        // US ZIP+4: 9 raw digits → "XXXXX-XXXX"
        if (postalCode.Length == 9 && postalCode.All(char.IsDigit))
        {
            return $"{postalCode[..5]}-{postalCode[5..]}";
        }

        // Canadian: 6 alphanumeric chars (no space) → "XXX XXX"
        if (postalCode.Length == 6 && char.IsLetter(postalCode[0]) && postalCode.All(char.IsLetterOrDigit))
        {
            return $"{postalCode[..3]} {postalCode[3..]}";
        }

        // 5-digit US ZIP or already-formatted code → return as-is
        return postalCode;
    }
}
