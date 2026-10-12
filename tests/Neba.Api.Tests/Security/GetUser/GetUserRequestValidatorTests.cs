using Neba.Api.Contracts.Security.GetUser;
using Neba.Api.Security.GetUser;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.GetUser;

[UnitTest]
[Component("Security")]
public sealed class GetUserRequestValidatorTests
{
    private readonly GetUserRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the user ID is a valid ULID")]
    public void Validate_ShouldSucceed_WhenUserIdIsValidUlid()
    {
        // Arrange
        var request = new GetUserRequest { UserId = Ulid.NewUlid().ToString() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with UserIdInvalid when the user ID is not a ULID")]
    public void Validate_ShouldFailWithUserIdInvalid_WhenUserIdIsNotUlid()
    {
        // Arrange
        var request = new GetUserRequest { UserId = "not-a-ulid" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "GetUserRequest.UserIdInvalid");
    }
}
