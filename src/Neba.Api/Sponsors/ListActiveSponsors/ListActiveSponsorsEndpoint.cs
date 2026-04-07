using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Sponsors;
using Neba.Application.Messaging;
using Neba.Application.Sponsors;
using Neba.Application.Sponsors.ListActiveSponsors;

namespace Neba.Api.Sponsors.ListActiveSponsors;

internal sealed class ListActiveSponsorsEndpoint(IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<SponsorSummaryResponse>>
{
    private readonly IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("active");
        Group<SponsorsGroup>();

        Options(options => options
            .WithVersionSet("Sponsors")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListActiveSponsors")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListActiveSponsorsQuery(), ct);

        var response = new CollectionResponse<SponsorSummaryResponse>
        {
            Items = [.. result
                .Select(s => new SponsorSummaryResponse
                {
                    Name = s.Name,
                    Slug = s.Slug,
                    LogoUrl = s.LogoUrl,
                    IsCurrentSponsor = s.IsCurrentSponsor,
                    Priority = s.Priority,
                    Tier = s.Tier,
                    Category = s.Category,
                    TagPhrase = s.TagPhrase,
                    Description = s.Description,
                    WebsiteUrl = s.WebsiteUrl,
                    FacebookUrl = s.FacebookUrl,
                    InstagramUrl = s.InstagramUrl,
                })],
        };

        await Send.OkAsync(response, ct);
    }
}
