using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class PrivateDataAttribute
    : DataClassificationAttribute
{
    public PrivateDataAttribute()
        : base(DataTaxonomy.PrivateData)
    { }
}