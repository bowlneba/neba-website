using Neba.Api.Contacts.Domain;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;

namespace Neba.Api.Tests.Features.BowlingCenters.Domain;

[UnitTest]
[Component("BowlingCenters")]
public sealed class BowlingCenterTests
{
    // ── EF owned-collection fixup regression ────────────────────────────────
    // Guards BowlingCenter.PhoneNumbers against reverting from `new List<PhoneNumber>()` back to the
    // `[]` collection-expression form (e.g. via an IDE0305/SonarQube "simplify collection
    // initialization" suggestion). Target-typed to the IReadOnlyCollection<PhoneNumber> property,
    // `[]` resolves to a fixed-size PhoneNumber[] at runtime, and EF's owned-collection fixup throws
    // NotSupportedException when it tries to Add into that array while materializing the aggregate.
    // See CLAUDE.md "EF Core Navigation Fixup" for the full writeup.

    [Fact(DisplayName = "PhoneNumbers property initializer default supports Add, as EF's owned-collection fixup requires")]
    public void PhoneNumbers_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup()
    {
        // Arrange
        var bowlingCenter = new BowlingCenter();
        var mutablePhoneNumbers = (ICollection<PhoneNumber>)bowlingCenter.PhoneNumbers;

        // Act & Assert — a fixed-size array throws NotSupportedException here instead
        Should.NotThrow(() => mutablePhoneNumbers.Add(PhoneNumberFactory.Create()));
    }
}
