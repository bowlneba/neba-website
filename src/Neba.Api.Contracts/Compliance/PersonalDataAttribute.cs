using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Contracts.Compliance;

/// <summary>Marks a property/parameter as identifying but low-risk; partially masked in logs/audits.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class PersonalDataAttribute
    : DataClassificationAttribute
{
    /// <summary>Initializes a new instance of <see cref="PersonalDataAttribute"/>.</summary>
    public PersonalDataAttribute()
        : base(DataTaxonomy.Personal)
    { }
}
