using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class PublicDataAttribute
    : DataClassificationAttribute
{
    public PublicDataAttribute()
        : base(DataTaxonomy.Public)
    { }
}