using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class PublicDataAttribute
    : DataClassificationAttribute
{
    public PublicDataAttribute()
        : base(DataTaxonomy.Public)
    { }
}