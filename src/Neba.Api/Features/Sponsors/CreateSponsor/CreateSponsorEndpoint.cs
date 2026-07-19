using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorEndpoint(Messaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor> commandHandler)
    : Endpoint<CreateSponsorRequest, SponsorResponse>
{
    private readonly Messaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<SponsorsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Sponsors")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateSponsor.PolicyName);

        Description(description => description
            .WithName("CreateSponsor")
            .WithTags("Admin")
            .Produces<SponsorResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateSponsorRequest req, CancellationToken ct)
    {
        var input = req.Sponsor;
        var contact = SponsorCommandMapper.MapContact(input.Contact);

        var command = new CreateSponsorCommand
        {
            Name = input.Name,
            Slug = input.Slug,
            IsCurrentSponsor = input.IsCurrentSponsor,
            Priority = input.Priority,
            Tier = SponsorTier.FromName(input.Tier),
            Category = SponsorCategory.FromName(input.Category),
            Logo = SponsorCommandMapper.MapLogo(input.Logo),
            WebsiteUrl = input.WebsiteUrl,
            TagPhrase = input.TagPhrase,
            Description = input.Description,
            LiveReadText = input.LiveReadText,
            PromotionalNotes = input.PromotionalNotes,
            FacebookUrl = input.FacebookUrl,
            InstagramUrl = input.InstagramUrl,
            BusinessStreet = input.BusinessStreet,
            BusinessUnit = input.BusinessUnit,
            BusinessCity = input.BusinessCity,
            BusinessState = SponsorCommandMapper.MapBusinessState(input.BusinessState),
            BusinessPostalCode = input.BusinessPostalCode,
            BusinessEmailAddress = input.BusinessEmailAddress,
            PhoneNumbers = SponsorCommandMapper.MapPhoneNumbers(input.PhoneNumbers),
            ContactName = contact.Name,
            ContactPhoneType = contact.PhoneType,
            ContactPhoneNumber = contact.PhoneNumber,
            ContactPhoneExtension = contact.PhoneExtension,
            ContactEmail = contact.Email
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);

                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);

            // Stryker disable once Statement
            return;
        }

        var response = new SponsorResponse
        {
            SponsorId = result.Value.Id.Value.ToString(),
            Slug = result.Value.Slug
        };

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetSponsorDetail",
            routeValues: new { slug = result.Value.Slug },
            responseBody: response,
            cancellation: ct);
    }
}