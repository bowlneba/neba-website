using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Api.Contracts.Contact;
using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Messaging;

namespace Neba.Api.BowlingCenters.ListBowlingCenters;

internal sealed class ListBowlingCentersEndpoint(IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<BowlingCenterSummaryResponse>>
{
    private readonly IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get(string.Empty);
        Group<BowlingCentersGroup>();

        Options(options => options
            .WithVersionSet("BowlingCenters")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListBowlingCenters")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListBowlingCentersQuery(), ct);

        var response = new CollectionResponse<BowlingCenterSummaryResponse>
        {
            Items = [.. result
                .Select(bc => new BowlingCenterSummaryResponse
                {
                    CertificationNumber = bc.CertificationNumber,
                    Name = bc.Name,
                    Status = bc.Status.Name,
                    Street = bc.Address.Street,
                    Unit = bc.Address.Unit,
                    City = bc.Address.City,
                    State = bc.Address.Region,
                    PostalCode = bc.Address.PostalCode,
                    Latitude = bc.Address.Latitude!.Value,
                    Longitude = bc.Address.Longitude!.Value,
                    PhoneNumbers = [.. bc.PhoneNumbers
                        .Select(p => new PhoneNumberResponse
                        {
                            PhoneNumberType = p.PhoneNumberType.Name,
                            PhoneNumber = p.Number,
                        })],
                })],
        };

        await Send.OkAsync(response, ct);
    }
}