using System.Text;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class EnvelopeTests
{
    // Saved values depend on this text: changing it makes them unreadable.
    [Theory(DisplayName = "The additional data should keep its exact text for each purpose")]
    [InlineData((int)EncryptionPurpose.Storage, "v1|BowlerPii|storage|abc123", TestDisplayName = "Storage")]
    [InlineData((int)EncryptionPurpose.Wire, "v1|BowlerPii|wire|abc123", TestDisplayName = "Wire")]
    public void AdditionalData_ShouldMatchTheDocumentedText_WhenGivenAPurpose(int purpose, string expected)
    {
        // Arrange

        // Act
        var result = Envelope.AdditionalData(BowlerPii.Name, (EncryptionPurpose)purpose, "abc123");

        // Assert
        Encoding.UTF8.GetString(result).ShouldBe(expected);
    }
}