using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.ReferenceData;

namespace Neba.TestFactory.ReferenceData;

public static class PhoneNumberTypeResponseFactory
{
    public static PhoneNumberTypeResponse Create(
        string? name = null,
        string? code = null)
        => new()
        {
            Name = name ?? PhoneNumberType.Home.Name,
            Code = code ?? PhoneNumberType.Home.Value
        };

    public static IReadOnlyCollection<PhoneNumberTypeResponse> CreateAll()
        => [.. PhoneNumberType.List.Select(type => new PhoneNumberTypeResponse { Name = type.Name, Code = type.Value })];
}