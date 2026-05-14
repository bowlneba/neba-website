namespace Neba.Api.Contacts;

internal sealed record PhoneNumberDto
{
    public required string PhoneNumberType { get; init; }

    public required string Number { get; init; }
}