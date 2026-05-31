using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neba.Api.Database;

// EF Core 10's snapshot generator emits b1.ComplexProperty(Type, string, string, Action<>) when
// it encounters a ComplexProperty on an owned entity, where b1 is the non-generic
// OwnedNavigationBuilder base class. That overload is missing from the public API. This extension
// bridges the gap so generated snapshot and designer files compile without modification.
#pragma warning disable EF1001
internal static class OwnedNavigationComplexPropertyExtension
{
    public static ComplexPropertyBuilder ComplexProperty(
        this OwnedNavigationBuilder builder,
        Type complexType,
        string propertyName,
        string complexTypeName,
        Action<ComplexPropertyBuilder> buildAction)
    {
        var entityTypeBuilder = new EntityTypeBuilder(builder.OwnedEntityType);
        var complexPropertyBuilder = entityTypeBuilder.ComplexProperty(complexType, propertyName, complexTypeName);
        buildAction(complexPropertyBuilder);
        return complexPropertyBuilder;
    }
}
#pragma warning restore EF1001
