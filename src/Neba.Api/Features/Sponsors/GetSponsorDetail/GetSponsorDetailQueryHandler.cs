using System.Linq.Expressions;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contacts;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>
{
    private readonly IQueryable<Sponsor> _sponsors = appDbContext.Sponsors.AsNoTracking();
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    private static readonly Expression<Func<Sponsor, SponsorRow>> ProjectRow = sponsor => new SponsorRow(
        sponsor.Id,
        sponsor.Name,
        sponsor.Slug,
        sponsor.Logo != null ? sponsor.Logo.Container : null,
        sponsor.Logo != null ? sponsor.Logo.Path : null,
        sponsor.Logo != null ? sponsor.Logo.ContentType : null,
        sponsor.Logo != null ? sponsor.Logo.SizeInBytes : null,
        sponsor.IsCurrentSponsor,
        sponsor.Priority,
        sponsor.Tier.Name,
        sponsor.Category.Name,
        sponsor.TagPhrase,
        sponsor.Description,
        sponsor.LiveReadText,
        sponsor.PromotionalNotes,
        sponsor.WebsiteUrl,
        sponsor.FacebookUrl,
        sponsor.InstagramUrl,
        sponsor.BusinessAddress != null
            ? new AddressDto
            {
                Street = sponsor.BusinessAddress.Street,
                Unit = sponsor.BusinessAddress.Unit,
                City = sponsor.BusinessAddress.City,
                Region = sponsor.BusinessAddress.Region,
                PostalCode = sponsor.BusinessAddress.PostalCode,
                Country = sponsor.BusinessAddress.Country
            }
            : null,
        sponsor.BusinessEmail != null ? sponsor.BusinessEmail.Value : null,
        sponsor.PhoneNumbers.Select(phoneNumber => new PhoneNumberDto
        {
            Number = phoneNumber.Number,
            PhoneNumberType = phoneNumber.Type.Name
        }).ToList(),
        sponsor.SponsorContact != null
            ? new SponsorContactRow(
                sponsor.SponsorContact.Name,
                sponsor.SponsorContact.Phone.Type.Name,
                sponsor.SponsorContact.Phone.Number,
                sponsor.SponsorContact.Email.Value)
            : null,
        sponsor.TournamentsSponsored.Select(tournamentSponsor => new TournamentRow(
            tournamentSponsor.Tournament.Id,
            tournamentSponsor.Tournament.Name,
            tournamentSponsor.Tournament.StartDate,
            tournamentSponsor.Tournament.EndDate,
            tournamentSponsor.TitleSponsor)).ToList());

    public async Task<ErrorOr<SponsorDetailDto>> HandleAsync(GetSponsorDetailQuery query, CancellationToken cancellationToken)
    {
        var row = await _sponsors
            .Where(sponsor => sponsor.Slug == query.Slug)
            .Select(ProjectRow)
            .SingleOrDefaultAsync(cancellationToken);

        // An inactive sponsor gets the same "not found" response as a nonexistent slug —
        // visible only to callers who can manage sponsors.
        return row is null || (!row.IsCurrentSponsor && !query.CallerHasSponsorManagementPermission)
            ? SponsorErrors.SponsorNotFound(query.Slug)
            : MapToDto(row, query, _fileStorageService);
    }

    private static SponsorDetailDto MapToDto(SponsorRow row, GetSponsorDetailQuery query, IFileStorageService fileStorageService)
    {
        var canManage = query.CallerHasSponsorManagementPermission;

        return new SponsorDetailDto
        {
            Id = row.Id,
            Name = row.Name,
            Slug = row.Slug,
            LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                ? fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                : null,
            LogoContainer = canManage ? row.LogoContainer : null,
            LogoPath = canManage ? row.LogoPath : null,
            LogoContentType = canManage ? row.LogoContentType : null,
            LogoSizeInBytes = canManage ? row.LogoSizeInBytes : null,
            IsCurrentSponsor = row.IsCurrentSponsor,
            Priority = row.Priority,
            Tier = row.Tier,
            Category = row.Category,
            TagPhrase = row.TagPhrase,
            Description = row.Description,
            LiveReadText = canManage ? row.LiveReadText : null,
            PromotionalNotes = canManage ? row.PromotionalNotes : null,
            WebsiteUrl = row.WebsiteUrl,
            FacebookUrl = row.FacebookUrl,
            InstagramUrl = row.InstagramUrl,
            BusinessAddress = row.BusinessAddress,
            BusinessEmailAddress = row.BusinessEmailAddress,
            PhoneNumbers = row.PhoneNumbers,
            Contact = canManage && row.Contact is not null
                ? new SponsorContactDto
                {
                    Name = row.Contact.Name,
                    Phone = new PhoneNumberDto
                    {
                        PhoneNumberType = row.Contact.PhoneNumberType,
                        Number = row.Contact.PhoneNumber
                    },
                    Email = row.Contact.Email
                }
                : null,
            TournamentsSponsored = [.. row.TournamentsSponsored
                .OrderByDescending(t => t.StartDate)
                .Select(t => new SponsorDetailTournamentDto
                {
                    TournamentId = t.TournamentId.Value.ToString(),
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    TitleSponsor = t.TitleSponsor
                })]
        };
    }

    private sealed record SponsorRow(
        SponsorId Id,
        string Name,
        string Slug,
        string? LogoContainer,
        string? LogoPath,
        string? LogoContentType,
        long? LogoSizeInBytes,
        bool IsCurrentSponsor,
        int Priority,
        string Tier,
        string Category,
        string? TagPhrase,
        string? Description,
        string? LiveReadText,
        string? PromotionalNotes,
        Uri? WebsiteUrl,
        Uri? FacebookUrl,
        Uri? InstagramUrl,
        AddressDto? BusinessAddress,
        string? BusinessEmailAddress,
        List<PhoneNumberDto> PhoneNumbers,
        SponsorContactRow? Contact,
        List<TournamentRow> TournamentsSponsored);

    private sealed record SponsorContactRow(string Name, string PhoneNumberType, string PhoneNumber, string Email);

    private sealed record TournamentRow(TournamentId TournamentId, string Name, DateOnly StartDate, DateOnly EndDate, bool TitleSponsor);
}