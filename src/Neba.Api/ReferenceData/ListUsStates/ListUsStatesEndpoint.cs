using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.ReferenceData;
using Neba.Api.Messaging;

namespace Neba.Api.ReferenceData.ListUsStates;

internal sealed class ListUsStatesEndpoint(IQueryHandler<ListUsStatesQuery, IReadOnlyCollection<UsStateDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<UsStateResponse>>
{
    private readonly IQueryHandler<ListUsStatesQuery, IReadOnlyCollection<UsStateDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("us-states");
        Group<ReferenceDataEndpointGroup>();

        Options(options => options
            .WithVersionSet("ReferenceData")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListUsStates")
            .WithTags("Public")
            .Produces<CollectionResponse<UsStateResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListUsStatesQuery(), ct);

        var response = new CollectionResponse<UsStateResponse>
        {
            Items = [.. result
                .Select(state => new UsStateResponse { Name = state.Name, Code = state.Code })],
        };

        await Send.OkAsync(response, ct);
    }
}
