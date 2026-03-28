using Ardalis.SmartEnum;

namespace Neba.Domain.Bowlers;

/// <summary>
/// Represents the gender of a bowler. This is currently a simple enumeration with two values (Male and Female), but it is implemented as a SmartEnum to allow for potential future expansion (e.g., adding non-binary or other gender options) without breaking existing code or database schema. The string value ("M" for Male, "F" for Female) is used for storage and comparison purposes.
/// </summary>
public sealed class Gender
    : SmartEnum<Gender, string>
{
    /// <summary>
    /// Represents a male bowler. Stored as "M" in the database.
    /// </summary>
    public static readonly Gender Male = new(nameof(Male), "M");

    /// <summary>
    /// Represents a female bowler. Stored as "F" in the database.
    /// </summary>
    public static readonly Gender Female = new(nameof(Female), "F");

    private Gender(string name, string value)
        : base(name, value)
    { }
}