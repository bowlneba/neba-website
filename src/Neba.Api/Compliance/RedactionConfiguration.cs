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
                .SetRedactor<ErasingRedactor>(new DataClassificationSet(DataTaxonomy.PrivateData)));
        }
    }
}