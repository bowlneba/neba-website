using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace Neba.Api.Contracts.FeatureManagement;

[FilterAlias("AllowedEmail")]
internal sealed class AllowedEmailFilter
    : IContextualFeatureFilter<AllowedEmailContext>
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, AllowedEmailContext appContext)
    {
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