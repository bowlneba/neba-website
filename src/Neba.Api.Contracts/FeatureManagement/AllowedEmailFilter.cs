using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace Neba.Api.Contracts.FeatureManagement;

/// <summary>
/// A feature filter that allows or denies access to a feature based on the user's email address.
/// </summary>
[FilterAlias("AllowedEmail")]
public sealed class AllowedEmailFilter
    : IContextualFeatureFilter<AllowedEmailContext>
{
    /// <inheritdoc />
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, AllowedEmailContext appContext)
    {
        ArgumentNullException.ThrowIfNull(featureFilterContext);
        ArgumentNullException.ThrowIfNull(appContext);
        
        if (string.IsNullOrEmpty(appContext.Email))
        {
            return Task.FromResult(false);
        }

        var settings = featureFilterContext.Parameters.Get<AllowedEmailFilterSettings>()
            ?? new();

        var allowed = settings.AllowedEmails.Contains(appContext.Email);

        return Task.FromResult(allowed);
    }
}