namespace Neba.Api.Contracts.FeatureManagement;

internal sealed record AllowedEmailContext
{
    public string? Email { get; init; }
}