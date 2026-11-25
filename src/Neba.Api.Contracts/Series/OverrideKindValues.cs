namespace Neba.Api.Contracts.Series;

/// <summary>
/// The SmartEnum values of an override kind, as commands send them. The API's OverrideKind and the website share these.
/// </summary>
public static class OverrideKindValues
{
    /// <summary>The value of the Add kind.</summary>
    public const int Add = 1;

    /// <summary>The value of the Clear kind.</summary>
    public const int Clear = 2;
}