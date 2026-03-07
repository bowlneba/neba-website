namespace Neba.Website.Server.Contact;

internal static class PhoneNumberFormatter
{
    public static string FormatForDisplay(string rawPhoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPhoneNumber);

        var extensionIndex = rawPhoneNumber.IndexOf('x', StringComparison.OrdinalIgnoreCase);
        var digits = extensionIndex >= 0
            ? rawPhoneNumber[..extensionIndex]
            : rawPhoneNumber;

        var extension = extensionIndex >= 0
            ? rawPhoneNumber[(extensionIndex + 1)..]
            : null;

        var formatted = FormatDigits(digits);

        return extension is not null
            ? $"{formatted} x{extension}"
            : formatted;
    }

    private static string FormatDigits(string digits)
    {
        // Strip leading country code "1" for North American numbers
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
    }
}
