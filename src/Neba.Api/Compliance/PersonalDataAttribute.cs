using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class PersonalDataAttribute
    : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(DataTaxonomy.Personal)
    { }
}