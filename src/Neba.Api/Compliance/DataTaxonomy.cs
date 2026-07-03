using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

internal static class DataTaxonomy
{
    private const string TaxonomyName = nameof(DataTaxonomy);

    public static DataClassification PrivateData
        => new(TaxonomyName, nameof(PrivateData));
}