using Neba.Api.Contacts.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.ReferenceData.ListPhoneNumberTypes;

internal sealed class ListPhoneNumberTypesQueryHandler
    : IQueryHandler<ListPhoneNumberTypesQuery, IReadOnlyCollection<PhoneNumberTypeDto>>
{
    public Task<IReadOnlyCollection<PhoneNumberTypeDto>> HandleAsync(ListPhoneNumberTypesQuery query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PhoneNumberTypeDto>>(
            [.. PhoneNumberType.List.Select(type => new PhoneNumberTypeDto { Name = type.Name, Code = type.Value })]);
}
