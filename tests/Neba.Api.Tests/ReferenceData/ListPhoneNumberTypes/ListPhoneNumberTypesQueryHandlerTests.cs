using Neba.Api.ReferenceData.ListPhoneNumberTypes;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ReferenceData.ListPhoneNumberTypes;

[UnitTest]
[Component("ReferenceData")]
public sealed class ListPhoneNumberTypesQueryHandlerTests
{
    [Fact(DisplayName = "HandleAsync should return all 4 phone number types")]
    public async Task HandleAsync_ShouldReturnAll4Types()
    {
        // Arrange
        var handler = new ListPhoneNumberTypesQueryHandler();

        // Act
        var result = await handler.HandleAsync(new ListPhoneNumberTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "HandleAsync should map name and code correctly")]
    public async Task HandleAsync_ShouldMapNameAndCode_Correctly()
    {
        // Arrange
        var handler = new ListPhoneNumberTypesQueryHandler();

        // Act
        var result = await handler.HandleAsync(new ListPhoneNumberTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        var mobile = result.Single(t => t.Code == "M");
        mobile.Name.ShouldBe("Mobile");
    }
}
