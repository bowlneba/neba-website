using Neba.Api.Contracts.Sponsors;

namespace Neba.Website.Server.Sponsors;

internal static class SponsorMappingExtensions
{
    extension(SponsorSummaryResponse response)
    {
        public SponsorSummaryViewModel ToViewModel() =>
            new()
            {
                SponsorId = response.SponsorId,
                Name = response.Name,
                Slug = response.Slug,
                LogoUrl = response.LogoUrl,
                IsCurrentSponsor = response.IsCurrentSponsor,
                Priority = response.Priority,
                Tier = response.Tier,
                Category = response.Category,
                TagPhrase = response.TagPhrase,
                Description = response.Description,
                WebsiteUrl = response.WebsiteUrl,
                FacebookUrl = response.FacebookUrl,
                InstagramUrl = response.InstagramUrl
            };
    }

    extension(SponsorDetailResponse response)
    {
        public SponsorDetailViewModel ToViewModel() =>
            new()
            {
                Id = response.Id,
                Slug = response.Slug,
                Name = response.Name,
                IsCurrentSponsor = response.IsCurrentSponsor,
                TierName = response.Tier,
                CategoryName = response.Category,
                LogoUrl = response.LogoUrl,
                WebsiteUrl = response.WebsiteUrl,
                Tagline = response.TagPhrase,
                AboutText = response.Description,
                LiveReadText = response.LiveReadText,
                PromotionalNotes = response.PromotionalNotes,
                FacebookUrl = response.FacebookUrl,
                InstagramUrl = response.InstagramUrl,
                BusinessStreet = response.BusinessStreet,
                BusinessUnit = response.BusinessUnit,
                BusinessCity = response.BusinessCity,
                BusinessState = response.BusinessState,
                BusinessPostalCode = response.BusinessPostalCode,
                BusinessCountry = response.BusinessCountry,
                ContactEmail = response.BusinessEmailAddress,
                PhoneNumbers = response.PhoneNumbers,
                Contact = response.Contact,
                TournamentsSponsored = [.. response.TournamentsSponsored.Select(t => new SponsorDetailTournamentViewModel
                {
                    TournamentId = t.TournamentId,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    TitleSponsor = t.TitleSponsor
                })],
            };
    }
}