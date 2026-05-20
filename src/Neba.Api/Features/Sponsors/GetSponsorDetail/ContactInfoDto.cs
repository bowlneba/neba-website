using Neba.Api.Contacts;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

/// <summary>
/// Represents the contact information for a sponsor, including the contact person's name, phone number, and email address. This information is used to provide users with a way to reach out to the sponsor for inquiries or support.
/// </summary>
public sealed record ContactInfoDto
{
    /// <summary>
    /// The name of the contact person for the sponsor. This is the primary point of contact for any communication related to the sponsorship, and it helps users know who they are reaching out to when contacting the sponsor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The phone number of the contact person for the sponsor. This should include the area code and any necessary extensions to ensure that users can successfully reach the contact person when they call. Providing a phone number allows for direct communication, which can be important for time-sensitive inquiries or support related to the sponsorship.
    /// </summary>
    public required PhoneNumberDto PhoneNumber { get; init; }

    /// <summary>
    /// The email address of the contact person for the sponsor. This allows users to send inquiries or requests for information via email, providing an alternative method of communication that may be more convenient for some users. The email address should be monitored regularly to ensure timely responses to any messages received from users regarding the sponsorship.
    /// </summary>
    public required string EmailAddress { get; init; }
}