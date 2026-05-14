using ErrorOr;

namespace Neba.Api.Features.BowlingCenters.Domain;

/// <summary>
/// Defines error messages related to validation of <see cref="CertificationNumber"/> instances.
/// </summary>
public static class CertificationNumberErrors
{
    /// <summary>
    /// Error indicating that a certification number cannot be null or empty.
    /// </summary>
    public static Error CertificationNumberNullOrEmpty
        => Error.Validation(
            code: "CertificationNumber.NullOrEmpty",
            description: "Certification number cannot be null or empty.");

    /// <summary>
    /// Error indicating that a certification number must contain only digits, as issued by USBC.
    /// </summary>
    public static Error CertificationNumberNotNumeric
        => Error.Validation(
            code: "CertificationNumber.NotNumeric",
            description: "Certification number must contain only digits.");
}