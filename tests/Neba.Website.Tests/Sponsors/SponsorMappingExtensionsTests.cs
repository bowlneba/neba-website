using Neba.Api.Contracts.Sponsors;
using Neba.Domain.Sponsors;
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
        var responses = SponsorSummaryResponseFactory.Bogus(3, seed: 1);

        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        await Verify(viewModels);
    }

    [Fact(DisplayName = "Maps nullable fields as null when not provided")]
    public void ToViewModel_ShouldMapNullableFieldsAsNull_WhenNotProvided()
    {
        // Constructed directly because the factory null-coalesces TagPhrase and Description to defaults,
        // making it impossible to produce a response with those fields null via the factory.
        var response = new SponsorSummaryResponse
        {
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

        var viewModel = response.ToViewModel();

        viewModel.LogoUrl.ShouldBeNull();
        viewModel.TagPhrase.ShouldBeNull();
        viewModel.Description.ShouldBeNull();
        viewModel.WebsiteUrl.ShouldBeNull();
        viewModel.FacebookUrl.ShouldBeNull();
        viewModel.InstagramUrl.ShouldBeNull();
    }
}
