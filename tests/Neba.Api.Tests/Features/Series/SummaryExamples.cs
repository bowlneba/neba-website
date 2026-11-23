using System.Reflection;

using FastEndpoints;

using Microsoft.AspNetCore.Http.Metadata;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Reads the example a Summary registered for a status code. FastEndpoints keeps it in an internal list, so this reads it by reflection.</summary>
internal static class SummaryExamples
{
    public static object? For(EndpointSummary summary, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var metas = typeof(EndpointSummary)
            .GetProperty("ProducesMetas", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(summary) as IEnumerable<IProducesResponseTypeMetadata>;

        metas.ShouldNotBeNull("FastEndpoints no longer exposes ProducesMetas; update SummaryExamples.");

        var meta = metas.Single(m => m.StatusCode == statusCode);

        return meta.GetType().GetProperty("Example", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(meta);
    }
}