namespace Neba.Api.Contracts.FeatureManagement;

internal sealed record AllowedEmailFilterSettings
{
    public IReadOnlyCollection<string> AllowedEmails { get; init; } = [];
}