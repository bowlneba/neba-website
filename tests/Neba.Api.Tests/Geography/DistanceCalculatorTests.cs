using Neba.Api.Geography;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;

namespace Neba.Api.Tests.Geography;

[UnitTest]
[Component("Geography")]
public sealed class DistanceCalculatorTests
{
    #region DistanceInMiles - valid

    [Fact(DisplayName = "Returns a positive distance when two addresses at different locations are provided")]
    public void DistanceInMiles_ShouldReturnPositiveDistance_WhenAddressesAreDifferent()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 40.7128, longitude: -74.0060));
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 42.3601, longitude: -71.0589));

        // Act
        var result = DistanceCalculator.DistanceInMiles(address1, address2);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Returns approximately 191 miles between New York City and Boston")]
    public void DistanceInMiles_ShouldReturnApproximateDistance_BetweenNewYorkCityAndBoston()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 40.7128, longitude: -74.0060));
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 42.3601, longitude: -71.0589));

        // Act
        var result = DistanceCalculator.DistanceInMiles(address1, address2);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeInRange(185m, 200m);
    }

    [Fact(DisplayName = "Returns zero when both addresses have identical coordinates")]
    public void DistanceInMiles_ShouldReturnZero_WhenBothAddressesHaveSameCoordinates()
    {
        // Arrange
        var coordinates = CoordinatesFactory.Create();
        var address1 = AddressFactory.CreateUsAddress(coordinates: coordinates);
        var address2 = AddressFactory.CreateUsAddress(coordinates: coordinates);

        // Act
        var result = DistanceCalculator.DistanceInMiles(address1, address2);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(0m);
    }

    #endregion

    #region DistanceInMiles - missing coordinates

    [Fact(DisplayName = "Returns AddressMissingCoordinates error when address1 has no coordinates")]
    public void DistanceInMiles_ShouldReturnError_WhenAddress1HasNoCoordinates()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress();
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create());

        // Act
        var result = DistanceCalculator.DistanceInMiles(address1, address2);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.DistanceCalculator.AddressMissingCoordinates");
    }

    [Fact(DisplayName = "Returns AddressMissingCoordinates error when address2 has no coordinates")]
    public void DistanceInMiles_ShouldReturnError_WhenAddress2HasNoCoordinates()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create());
        var address2 = AddressFactory.CreateUsAddress();

        // Act
        var result = DistanceCalculator.DistanceInMiles(address1, address2);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.DistanceCalculator.AddressMissingCoordinates");
    }

    [Fact(DisplayName = "Returns AddressMissingCoordinates error when both addresses have no coordinates")]
    public void DistanceInMiles_ShouldReturnError_WhenBothAddressesHaveNoCoordinates()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress();
        var address2 = AddressFactory.CreateUsAddress();

        // Act
        var result = DistanceCalculator.DistanceInMiles(address1, address2);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.DistanceCalculator.AddressMissingCoordinates");
    }

    #endregion

    [Fact(DisplayName = "Returns the same distance regardless of the order the addresses are given")]
    public void DistanceInMiles_ShouldReturnSameDistance_WhenAddressOrderIsReversed()
    {
        // Arrange
        var newYork = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 40.7128, longitude: -74.0060));
        var boston = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 42.3601, longitude: -71.0589));

        // Act
        var forwardResult = DistanceCalculator.DistanceInMiles(newYork, boston);
        var reverseResult = DistanceCalculator.DistanceInMiles(boston, newYork);

        // Assert
        forwardResult.Value.ShouldBe(reverseResult.Value);
    }

    #region DistanceInMiles - null guards

#nullable disable
    [Fact(DisplayName = "Throws ArgumentNullException when address1 is null")]
    public void DistanceInMiles_ShouldThrow_WhenAddress1IsNull()
    {
        // Arrange
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DistanceCalculator.DistanceInMiles(null, address2));
    }

    [Fact(DisplayName = "Throws ArgumentNullException when address2 is null")]
    public void DistanceInMiles_ShouldThrow_WhenAddress2IsNull()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DistanceCalculator.DistanceInMiles(address1, null));
    }
#nullable enable

    #endregion

    #region DistanceInKilometers - valid

    [Fact(DisplayName = "Returns a positive distance in kilometers when addresses are at different locations")]
    public void DistanceInKilometers_ShouldReturnPositiveDistance_WhenAddressesAreDifferent()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 40.7128, longitude: -74.0060));
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 42.3601, longitude: -71.0589));

        // Act
        var result = DistanceCalculator.DistanceInKilometers(address1, address2);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Returns a greater value in kilometers than miles for the same two addresses")]
    public void DistanceInKilometers_ShouldBeGreaterThanMiles_ForSameAddresses()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 40.7128, longitude: -74.0060));
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create(latitude: 42.3601, longitude: -71.0589));

        // Act
        var miles = DistanceCalculator.DistanceInMiles(address1, address2);
        var kilometers = DistanceCalculator.DistanceInKilometers(address1, address2);

        // Assert
        kilometers.Value.ShouldBeGreaterThan(miles.Value);
    }

    #endregion

    #region DistanceInKilometers - missing coordinates

    [Fact(DisplayName = "Returns AddressMissingCoordinates error when address1 has no coordinates")]
    public void DistanceInKilometers_ShouldReturnError_WhenAddress1HasNoCoordinates()
    {
        // Arrange
        var address1 = AddressFactory.CreateUsAddress();
        var address2 = AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create());

        // Act
        var result = DistanceCalculator.DistanceInKilometers(address1, address2);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.DistanceCalculator.AddressMissingCoordinates");
    }

    #endregion
}
