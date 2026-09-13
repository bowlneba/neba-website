using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Contracts.Compliance;

/// <summary>Marks a property/parameter as sensitive PII; fully redacted to empty in logs/audits.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class PrivateDataAttribute
    : DataClassificationAttribute
{
    /// <summary>Initializes a new instance of <see cref="PrivateDataAttribute"/>.</summary>
    public PrivateDataAttribute()
        : base(DataTaxonomy.Private)
    { }
}