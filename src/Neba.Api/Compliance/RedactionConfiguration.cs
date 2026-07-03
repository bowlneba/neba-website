using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Neba.Api.Compliance;

internal static class RedactionConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public void AddRedaction()
        {
            builder.Services.AddRedaction(options => options
                .SetRedactor<NullRedactor>(new DataClassificationSet(DataTaxonomy.Public))
                .SetRedactor<StarMaskingRedactor>(new DataClassificationSet(DataTaxonomy.Personal))
                .SetRedactor<ErasingRedactor>(new DataClassificationSet(DataTaxonomy.Private)));

            // [LoggerMessage] classified parameters are only redacted when the logging
            // pipeline itself is wrapped by ExtendedLogger; AddRedaction() alone only
            // registers IRedactorProvider and has no effect on ILogger.Log calls.
            builder.Logging.EnableRedaction();
        }
    }
}