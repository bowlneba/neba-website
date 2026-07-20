using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.ReferenceData.ListPhoneNumberTypes;

internal sealed record ListPhoneNumberTypesQuery
    : ICachedQuery<IReadOnlyCollection<PhoneNumberTypeDto>>
{
    public CacheDescriptor Cache => CacheDescriptors.ReferenceData.PhoneNumberTypes;

    public TimeSpan Expiry => TimeSpan.FromDays(30);
}