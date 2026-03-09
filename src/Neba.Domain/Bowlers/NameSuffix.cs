using Ardalis.SmartEnum;

namespace Neba.Domain.Bowlers;

/// <summary>
/// Represents a name suffix for a bowler, used in legal and official contexts such as 1099 tax reporting.
/// </summary>
public sealed class NameSuffix
    : SmartEnum<NameSuffix,string>
{
    /// <summary>
    /// Indicates that the bowler is a junior (e.g., John Doe Jr.). This suffix is used to distinguish a son from his father when they share the same name.
    /// </summary>
    public static readonly NameSuffix Jr = new(nameof(Jr), "Jr.");

    /// <summary>
    /// Indicates that the bowler is a senior (e.g., John Doe Sr.). This suffix is used to distinguish a father from his son when they share the same name.
    /// </summary>
    public static readonly NameSuffix Sr = new(nameof(Sr), "Sr.");

    /// <summary>
    /// Indicates that the bowler is the second in a family line with the same name (e.g., John Doe II). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather.
    /// </summary>
    public static readonly NameSuffix II = new(nameof(II), "II");

    /// <summary>
    /// Indicates that the bowler is the third in a family line with the same name (e.g., John Doe III). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather, and there are already two previous generations with the same name.
    /// </summary>
    public static readonly NameSuffix III = new(nameof(III), "III");

    /// <summary>
    /// Indicates that the bowler is the fourth in a family line with the same name (e.g., John Doe IV). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather, and there are already three previous generations with the same name.
    /// </summary>
    public static readonly NameSuffix IV = new(nameof(IV), "IV");

    /// <summary>
    /// Indicates that the bowler is the fifth in a family line with the same name (e.g., John Doe V). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather, and there are already four previous generations with the same name.
    /// </summary>
    public static readonly NameSuffix V = new(nameof(V), "V");

    private NameSuffix(string name, string value)
        : base(name, value)
    { }
}
