using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.ReferenceData;

namespace Neba.TestFactory.ReferenceData;

public static class UsStateResponseFactory
{
    public static UsStateResponse Create(
        string? name = null,
        string? code = null)
        => new()
        {
            Name = name ?? UsState.Connecticut.Name,
            Code = code ?? UsState.Connecticut.Value
        };

    public static IReadOnlyCollection<UsStateResponse> CreateAll()
        => [.. UsState.List.Select(state => new UsStateResponse { Name = state.Name, Code = state.Value })];
}
