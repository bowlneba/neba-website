namespace Neba.Api.Contracts.FeatureManagement;

/// <summary>
/// Settings for the AllowedEmail feature filter, containing the list of allowed email addresses.
/// </summary>
internal sealed record AllowedEmailFilterSettings
{
    public IReadOnlyCollection<string> AllowedEmails { get; init; } = [];
}