namespace Neba.Api.Contracts.FeatureManagement;

/// <summary>
/// Represents the context for the AllowedEmail feature filter, containing the user's email address.
/// </summary>
public sealed record AllowedEmailContext
{
    /// <summary>
    /// Gets or sets the email address of the user for whom the feature is being evaluated.
    /// </summary>
    public string? Email { get; init; }
}