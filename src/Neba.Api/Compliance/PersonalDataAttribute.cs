using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class PersonalDataAttribute
    : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(DataTaxonomy.Personal)
    { }
}