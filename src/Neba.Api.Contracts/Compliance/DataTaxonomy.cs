using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Contracts.Compliance;

/// <summary>
/// Data-sensitivity classifications shared by every DTO/property tagged with
/// <see cref="PublicDataAttribute"/>/<see cref="PersonalDataAttribute"/>/<see cref="PrivateDataAttribute"/>.
/// Lives in the Contracts project (rather than Neba.Api) so request/response DTOs shared across
/// the API/website boundary can carry these attributes directly.
/// </summary>
public static class DataTaxonomy
{
    private const string TaxonomyName = nameof(DataTaxonomy);

    /// <summary>Not sensitive. Logged as-is, no redaction applied.</summary>
    public static DataClassification Public
        => new(TaxonomyName, nameof(Public));

    /// <summary>Identifying but low-risk on its own. Partially masked (first character kept, remainder starred out).</summary>
    public static DataClassification Personal
        => new(TaxonomyName, nameof(Personal));

    /// <summary>Sensitive PII. Fully redacted to an empty string.</summary>
    public static DataClassification Private
        => new(TaxonomyName, nameof(Private));
}
