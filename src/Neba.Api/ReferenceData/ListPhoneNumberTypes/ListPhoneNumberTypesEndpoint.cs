using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.ReferenceData;
using Neba.Api.Messaging;

namespace Neba.Api.ReferenceData.ListPhoneNumberTypes;

internal sealed class ListPhoneNumberTypesEndpoint(IQueryHandler<ListPhoneNumberTypesQuery, IReadOnlyCollection<PhoneNumberTypeDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<PhoneNumberTypeResponse>>
{
    private readonly IQueryHandler<ListPhoneNumberTypesQuery, IReadOnlyCollection<PhoneNumberTypeDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("phone-number-types");
        Group<ReferenceDataEndpointGroup>();

        Options(options => options
            .WithVersionSet("ReferenceData")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListPhoneNumberTypes")
            .WithTags("Public")
            .Produces<CollectionResponse<PhoneNumberTypeResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListPhoneNumberTypesQuery(), ct);

        var response = new CollectionResponse<PhoneNumberTypeResponse>
        {
            Items = [.. result
                .Select(type => new PhoneNumberTypeResponse { Name = type.Name, Code = type.Code })],
        };

        await Send.OkAsync(response, ct);
    }
}