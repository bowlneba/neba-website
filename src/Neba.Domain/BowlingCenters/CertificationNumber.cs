using ErrorOr;

namespace Neba.Domain.BowlingCenters;

/// <summary>
/// Represents a certification number for a bowling center, which may be a unique identifier or a placeholder value.
/// </summary>
public sealed record CertificationNumber
{
    /// <summary>
    /// Gets the certification number value, which must be non-empty and cannot start with 'x' (reserved for placeholders).
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Indicates whether the certification number is a placeholder (starts with 'x') or a valid certification number.
    /// </summary>
    public bool IsPlaceholder
        => Value.StartsWith('x');

    /// <summary>
    /// Creates a new placeholder <see cref="CertificationNumber"/> with the specified sequence, which must be non-empty and will be prefixed with 'x' to indicate it is a placeholder rather than a valid certification number.
    /// </summary>
    /// <param name="sequence">The sequence to use for the placeholder certification number.</param>
    /// <returns>An <see cref="ErrorOr{CertificationNumber}"/> containing either a valid placeholder <see cref="CertificationNumber"/> or an error if the input is invalid.</returns>
    public static ErrorOr<CertificationNumber> Placeholder(string sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return CertificationNumberErrors.CertificationNumberNullOrEmpty;
        }

        return new CertificationNumber { Value = $"x{sequence}" };
    }

    /// <summary>
    /// Creates a new <see cref="CertificationNumber"/> from the provided string value, validating that it is not null, empty, or a reserved placeholder.
    /// </summary> <param name="number">The certification number string to create.</param>
    /// <returns>An <see cref="ErrorOr{CertificationNumber}"/> containing either a valid <see cref="CertificationNumber"/> or an error if the input is invalid.</returns>
    public static ErrorOr<CertificationNumber> Create(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return CertificationNumberErrors.CertificationNumberNullOrEmpty;
        }

        if (number.StartsWith('x'))
        {
            return CertificationNumberErrors.CertificationNumberStartsWithX;
        }

        return new CertificationNumber { Value = number };
    }
}

/// <summary>
/// Defines error messages related to validation of <see cref="CertificationNumber"/> instances, such as null/empty values or reserved placeholder formats.
/// </summary>
public static class CertificationNumberErrors
{
    /// <summary>
    /// Error indicating that a certification number cannot be null or empty, which is required for valid certification numbers.
    /// </summary>
    public static Error CertificationNumberNullOrEmpty
        => Error.Validation(
            code: "CertificationNumber.NullOrEmpty",
            description: "Certification number cannot be null or empty.");

    /// <summary>
    /// Error indicating that a certification number cannot start with 'x', as this prefix is reserved for placeholder values and not valid certification numbers.
    /// </summary>
    public static Error CertificationNumberStartsWithX
        => Error.Validation(
            code: "CertificationNumber.StartsWithX",
            description: "Certification number cannot start with 'x' as it is reserved for placeholders.");
}