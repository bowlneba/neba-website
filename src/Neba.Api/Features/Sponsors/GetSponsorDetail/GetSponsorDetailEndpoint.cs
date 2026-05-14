using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Sponsors;
using Neba.Application.Messaging;
using Neba.Application.Sponsors.GetSponsorDetail;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailEndpoint(IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>> queryHandler)
    : Endpoint<GetSponsorDetailRequest, SponsorDetailResponse>
{
    private readonly IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{slug}");
        Group<SponsorsGroup>();

        Options(options => options
            .WithVersionSet("Sponsors")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetSponsorDetail")
            .WithTags("Public")
            .Produces<SponsorDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetSponsorDetailRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new GetSponsorDetailQuery { Slug = req.Slug }, ct);

        if (result.IsError)
        {
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        var response = new SponsorDetailResponse
        {
            Id = dto.Id.Value,
            Name = dto.Name,
            Slug = dto.Slug,
            IsCurrentSponsor = dto.IsCurrentSponsor,
            Priority = dto.Priority,
            Tier = dto.Tier,
            Category = dto.Category,
            LogoUrl = dto.LogoUrl,
            WebsiteUrl = dto.WebsiteUrl,
            TagPhrase = dto.TagPhrase,
            Description = dto.Description,
            PromotionalNotes = dto.PromotionalNotes,
            LiveReadText = dto.LiveReadText,
            FacebookUrl = dto.FacebookUrl,
            InstagramUrl = dto.InstagramUrl,
            BusinessStreet = dto.BusinessAddress?.Street,
            BusinessUnit = dto.BusinessAddress?.Unit,
            BusinessCity = dto.BusinessAddress?.City,
            BusinessState = dto.BusinessAddress?.Region,
            BusinessPostalCode = dto.BusinessAddress?.PostalCode,
            BusinessCountry = dto.BusinessAddress?.Country,
            BusinessEmailAddress = dto.BusinessEmailAddress,
            PhoneNumbers = [.. dto.PhoneNumbers.Select(p => new PhoneNumberResponse
            {
                PhoneNumberType = p.PhoneNumberType,
                PhoneNumber = p.Number,
            })],
            SponsorContactName = dto.SponsorContactInfo?.Name,
            SponsorContactEmailAddress = dto.SponsorContactInfo?.EmailAddress,
            SponsorContactPhoneNumber = dto.SponsorContactInfo?.PhoneNumber.Number,
            SponsorContactPhoneNumberType = dto.SponsorContactInfo?.PhoneNumber.PhoneNumberType,
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}