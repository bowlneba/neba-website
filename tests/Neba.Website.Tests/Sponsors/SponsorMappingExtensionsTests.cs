using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Sponsors;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.SponsorMappingExtensions")]
public sealed class SponsorMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModel_ShouldMapAllFields()
    {
        // Arrange
        var responses = SponsorSummaryResponseFactory.Bogus(3, seed: 1);

        // Act
        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        // Assert
        await Verify(viewModels);
    }

    [Fact(DisplayName = "Maps nullable fields as null when not provided")]
    public void ToViewModel_ShouldMapNullableFieldsAsNull_WhenNotProvided()
    {
        // Arrange — constructed directly because the factory null-coalesces TagPhrase and Description to defaults,
        // making it impossible to produce a response with those fields null via the factory.
        var response = new SponsorSummaryResponse
        {
            SponsorId = SponsorSummaryResponseFactory.ValidSponsorId,
            Name = SponsorSummaryResponseFactory.ValidName,
            Slug = SponsorSummaryResponseFactory.ValidSlug,
            LogoUrl = null,
            IsCurrentSponsor = true,
            Priority = 1,
            Tier = SponsorTier.Standard.Name,
            Category = SponsorCategory.Technology.Name,
            TagPhrase = null,
            Description = null,
            WebsiteUrl = null,
            FacebookUrl = null,
            InstagramUrl = null
        };

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.LogoUrl.ShouldBeNull();
        viewModel.TagPhrase.ShouldBeNull();
        viewModel.Description.ShouldBeNull();
        viewModel.WebsiteUrl.ShouldBeNull();
        viewModel.FacebookUrl.ShouldBeNull();
        viewModel.InstagramUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "Maps LiveReadText, PromotionalNotes, and Contact from detail response to view model")]
    public void ToViewModel_ShouldMapLiveReadTextPromotionalNotesAndContact_FromDetailResponse()
    {
        // Arrange
        var contact = SponsorContactResponseFactory.Create();
        var response = SponsorDetailResponseFactory.Create(
            liveReadText: "Read this live!",
            promotionalNotes: "Internal notes",
            contact: contact);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.LiveReadText.ShouldBe("Read this live!");
        viewModel.PromotionalNotes.ShouldBe("Internal notes");
        viewModel.Contact.ShouldBe(contact);
    }

    [Fact(DisplayName = "Maps LiveReadText, PromotionalNotes, and Contact as null from detail response when not provided")]
    public void ToViewModel_ShouldMapLiveReadTextPromotionalNotesAndContactAsNull_WhenNotProvidedOnDetailResponse()
    {
        // Arrange
        var response = SponsorDetailResponseFactory.Create();

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.LiveReadText.ShouldBeNull();
        viewModel.PromotionalNotes.ShouldBeNull();
        viewModel.Contact.ShouldBeNull();
    }

    [Fact(DisplayName = "Maps TournamentsSponsored from detail response to view model")]
    public void ToViewModel_ShouldMapTournamentsSponsored_FromDetailResponse()
    {
        // Arrange
        var tournament = SponsorDetailTournamentResponseFactory.Create(
            name: "NEBA Championship",
            titleSponsor: true);
        var response = SponsorDetailResponseFactory.Create(tournamentsSponsored: [tournament]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.TournamentsSponsored.ShouldHaveSingleItem();
        var mapped = viewModel.TournamentsSponsored.Single();
        mapped.TournamentId.ShouldBe(tournament.TournamentId);
        mapped.Name.ShouldBe(tournament.Name);
        mapped.StartDate.ShouldBe(tournament.StartDate);
        mapped.EndDate.ShouldBe(tournament.EndDate);
        mapped.TitleSponsor.ShouldBeTrue();
    }
}