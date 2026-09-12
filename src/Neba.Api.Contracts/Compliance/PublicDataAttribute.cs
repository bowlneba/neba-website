using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Contracts.Compliance;

/// <summary>Marks a property/parameter as not sensitive; passes through logs/audits unredacted.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class PublicDataAttribute
    : DataClassificationAttribute
{
    /// <summary>Initializes a new instance of <see cref="PublicDataAttribute"/>.</summary>
    public PublicDataAttribute()
        : base(DataTaxonomy.Public)
    { }
}
