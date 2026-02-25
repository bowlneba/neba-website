using System.Globalization;

using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class BowlingCenterIdTests
{
    [Fact(DisplayName = "Should be a valid strongly typed ID")]
    public void BowlingCenterId_ShouldBeValidStronglyTypedId()
    {
        var id = BowlingCenterId.New();

        id.Value.ShouldNotBe(Ulid.Empty);
        BowlingCenterId.Parse(id.ToString(), CultureInfo.InvariantCulture).ShouldBe(id);
    }
}
