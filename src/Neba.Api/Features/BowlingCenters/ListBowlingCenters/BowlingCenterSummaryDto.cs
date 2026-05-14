using Neba.Application.Contact;

namespace Neba.Api.Features.BowlingCenters.ListBowlingCenters;

internal sealed record BowlingCenterSummaryDto
{
    public required string CertificationNumber { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required AddressDto Address { get; init; }

    public IReadOnlyCollection<PhoneNumberDto> PhoneNumbers { get; init; } = [];

    public string? Website { get; init; }
}