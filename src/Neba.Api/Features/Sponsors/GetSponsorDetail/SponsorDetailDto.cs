using Neba.Api.Contacts;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

/// <summary>
/// Data transfer object representing the detailed information of a sponsor, including its basic details, contact information, and promotional content. This DTO is used to transfer sponsor data from the application layer to the presentation layer when retrieving sponsor details.
/// </summary>
public sealed record SponsorDetailDto
{
    /// <summary>
    /// Unique identifier for the sponsor. This is used to uniquely identify the sponsor in the system and can be used for referencing the sponsor in various operations, such as retrieving details, updating information, or associating the sponsor with events or promotions.
    /// </summary>
    public required SponsorId Id { get; init; }

    /// <summary>
    /// Name of the sponsor. This is the official name of the sponsor as it should be displayed to users. It helps users recognize the sponsor and can be used in various parts of the application, such as listings, detail pages, and promotional materials.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly identifier for the sponsor. This is typically a lowercase, hyphen-separated version of the sponsor's name that can be used in URLs to reference the sponsor's detail page. It helps improve SEO and provides a user-friendly way to access sponsor information via the web.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Indicates whether the sponsor is a current sponsor. This boolean value helps differentiate between active sponsors who are currently supporting the organization and past sponsors who may have supported in the past but are no longer active. This information can be used to filter sponsors in listings and to provide context to users about the sponsor's current relationship with the organization.
    /// </summary>
    public required bool IsCurrentSponsor { get; init; }

    /// <summary>
    /// Priority of the sponsor. This integer value can be used to determine the order in which sponsors are displayed in listings or promotional materials. A lower number typically indicates a higher priority, meaning that sponsors with a priority of 1 would be displayed before those with a priority of 2, and so on. This allows the organization to highlight certain sponsors based on their level of support or importance.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Tier of the sponsor. This string value represents the sponsorship tier (e.g., "Platinum", "Gold", "Silver") that indicates the level of support provided by the sponsor. The tier can be used to categorize sponsors and to provide users with an understanding of the sponsor's contribution to the organization. It can also be used for filtering and sorting sponsors in listings and promotional materials.
    /// </summary>
    public required string Tier { get; init; }

    /// <summary>
    /// Category of the sponsor. This string value represents the category or industry that the sponsor belongs to (e.g., "Technology", "Finance", "Education"). The category can be used to group sponsors by their industry or area of focus, making it easier for users to find sponsors that are relevant to their interests or needs. It can also be used for filtering and sorting sponsors in listings and promotional materials.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Blob storage container name where the logo is stored.
    /// </summary>
    public string? LogoContainer { get; init; }

    /// <summary>
    /// Blob storage path to the sponsor logo.
    /// </summary>
    public string? LogoPath { get; init; }

    /// <summary>
    /// URL of the sponsor's logo. This is the publicly accessible URL where the sponsor's logo image can be accessed. It can be used in the presentation layer to display the sponsor's logo without needing to retrieve the file content directly from storage. This allows for efficient loading of images and reduces the need for additional API calls to fetch the logo content.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Website URL of the sponsor. This is the official website of the sponsor, where users can learn more about the sponsor's products, services, and involvement with the organization. Providing a website URL allows users to easily access additional information about the sponsor and can help drive traffic to the sponsor's site, which can be beneficial for both the sponsor and the organization.
    /// </summary>
    public Uri? WebsiteUrl { get; init; }

    /// <summary>
    /// Tagline or slogan of the sponsor. This is a brief phrase that captures the essence of the sponsor's brand or message. It can be used in promotional materials and listings to provide users with a quick understanding of what the sponsor represents or offers. A well-crafted tagline can enhance the sponsor's appeal and make it more memorable to users.
    /// </summary>
    public string? TagPhrase { get; init; }

    /// <summary>
    /// Description of the sponsor. This is a more detailed text that provides information about the sponsor, such as its history, mission, products, services, and involvement with the organization. The description can be used on the sponsor's detail page to give users a comprehensive understanding of the sponsor and its relationship with the organization. It can also be used in promotional materials to highlight the sponsor's contributions and offerings.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Promotional notes about the sponsor. This is a free-form text field that can contain additional information about the sponsor, such as special offers, upcoming events, or any other promotional content that the sponsor wants to share with users. This information can be used to entice users to engage with the sponsor and can be displayed in various parts of the application to highlight the sponsor's offerings and involvement with the organization.
    /// </summary>
    public string? PromotionalNotes { get; init; }

    /// <summary>
    /// Live read text for the sponsor. This is a script or set of talking points that can be used by presenters or hosts when mentioning the sponsor during live events, such as webinars, conferences, or podcasts. The live read text should be concise and engaging, providing key information about the sponsor and encouraging the audience to learn more or take action. It can be used to ensure that sponsors receive consistent and effective promotion during live events.
    /// </summary>
    public string? LiveReadText { get; init; }

    /// <summary>
    /// Facebook URL of the sponsor. This is the URL to the sponsor's official Facebook page. Providing a Facebook URL allows users to easily connect with the sponsor on social media, where they can follow updates, engage with content, and learn more about the sponsor's involvement with the organization. It can also help increase the sponsor's social media presence and foster a sense of community among users who are interested in the sponsor.
    /// </summary>
    public Uri? FacebookUrl { get; init; }

    /// <summary>
    /// Instagram URL of the sponsor. This is the URL to the sponsor's official Instagram profile. Providing an Instagram URL allows users to easily connect with the sponsor on social media, where they can view photos, videos, and updates about the sponsor's involvement with the organization. It can also help increase the sponsor's social media presence and foster a sense of community among users who are interested in the sponsor.
    /// </summary>
    public Uri? InstagramUrl { get; init; }

    /// <summary>
    /// Business address of the sponsor. This is the physical address of the sponsor's business location. Providing a business address can help users understand where the sponsor is located and can be useful for local sponsors who want to attract nearby customers or clients. It can also be used in promotional materials to highlight the sponsor's presence in a particular region or community.
    /// </summary>
    public AddressDto? BusinessAddress { get; init; }

    /// <summary>
    /// Contact information for the sponsor. This includes the name, phone number, and email address of the primary contact person for the sponsor. Providing contact information allows users to reach out to the sponsor directly for inquiries, support, or further engagement related to the sponsorship. It can also help foster communication between the sponsor and users who are interested in learning more about the sponsor's offerings or involvement with the organization.
    /// </summary>
    public string? BusinessEmailAddress { get; init; }

    /// <summary>
    /// Phone numbers of the sponsor. This is a collection of phone numbers that users can use to contact the sponsor. Providing multiple phone numbers allows users to choose the most appropriate number for their needs, such as a general inquiry line, a support line, or a sales line. It can also help ensure that users can successfully reach the sponsor, even if one of the phone numbers is unavailable or busy.
    /// </summary>
    public IReadOnlyCollection<PhoneNumberDto> PhoneNumbers { get; init; } = [];

    /// <summary>
    /// Contact information for the sponsor. This includes the name, phone number, and email address of the primary contact person for the sponsor. Providing contact information allows users to reach out to the sponsor directly for inquiries, support, or further engagement related to the sponsorship. It can also help foster communication between the sponsor and users who are interested in learning more about the sponsor's offerings or involvement with the organization.
    /// </summary>
    public ContactInfoDto? SponsorContactInfo { get; init; }
}