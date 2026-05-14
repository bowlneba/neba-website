namespace Neba.Website.Server.Contact;

internal static class PhoneNumberFormatter
{
    public static string FormatForDisplay(string rawPhoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPhoneNumber);

        var extensionIndex = rawPhoneNumber.IndexOf('x', StringComparison.OrdinalIgnoreCase);
        // Stryker disable once Conditional : rawPhoneNumber[..-1] throws; MTP runner swallows the exception
        var digits = extensionIndex >= 0
            ? rawPhoneNumber[..extensionIndex]
            : rawPhoneNumber;

        // Stryker disable once Equality : extensionIndex=0 → digits="" → FormatDigits returns null → raw returned regardless
        var extension = extensionIndex >= 0
            ? rawPhoneNumber[(extensionIndex + 1)..]
            : null;

        var formatted = FormatDigits(digits);

        if (formatted is null)
        {
            return rawPhoneNumber;
        }

        return extension is not null
            ? $"{formatted} x{extension}"
            : formatted;
    }

    private static string? FormatDigits(string digits)
    {
        digits = new string([.. digits.Where(char.IsDigit)]);

        // Strip leading country code "1" for North American numbers
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        return digits.Length != 10
            ? null
            : $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
    }
}