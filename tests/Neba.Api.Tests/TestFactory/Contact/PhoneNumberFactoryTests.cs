using Neba.Api.Contacts.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;

namespace Neba.Api.Tests.TestFactory.Contact;

[UnitTest]
[Component("Sponsors")]
public sealed class PhoneNumberFactoryTests
{
    [Fact(DisplayName = "Bogus should return requested count when count exceeds phone number types")]
    public void Bogus_ShouldReturnRequestedCount_WhenCountExceedsPhoneNumberTypes()
    {
        // Arrange
        const int count = 10;
        const int seed = 90;

        // Act
        var result = PhoneNumberFactory.Bogus(count, seed);

        // Assert
        result.Count.ShouldBe(count);
        result.ShouldAllBe(phone => phone.CountryCode == PhoneNumberFactory.ValidCountryCode);
        result.Select(phone => phone.Type).Distinct().Count().ShouldBeLessThanOrEqualTo(PhoneNumberType.List.Count);
    }

    [Fact(DisplayName = "Bogus should create requested contact infos when count exceeds phone number types")]
    public void Bogus_ShouldCreateRequestedContactInfos_WhenCountExceedsPhoneNumberTypes()
    {
        // Arrange
        const int count = 10;
        const int seed = 90;

        // Act
        var result = ContactInfoFactory.Bogus(count, seed);

        // Assert
        result.Count.ShouldBe(count);
        result.ShouldAllBe(contact => contact.Phone.CountryCode == PhoneNumberFactory.ValidCountryCode);
    }
}