using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database.Configurations;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("EmailAddress")]
public sealed class EmailAddressConfigurationTests
{
    private readonly IProperty _valueProperty;

    public EmailAddressConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new TestDbContext(options);
        var owner = context.Model.FindEntityType(typeof(TestOwner))!;
        var complexProperty = owner.FindComplexProperty(nameof(TestOwner.EmailAddress))!;
        _valueProperty = complexProperty.ComplexType.FindProperty(nameof(EmailAddress.Value))!;
    }

    [Fact(DisplayName = "email_address is varchar(255), not nullable")]
    public void HasEmailAddress_ShouldConfigureValueColumn()
    {
        _valueProperty.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("email_address");
        _valueProperty.GetMaxLength().ShouldBe(255);
        _valueProperty.IsNullable.ShouldBeFalse();
    }

    private sealed class TestOwner
    {
        public int Id { get; init; } = 5;
        public EmailAddress EmailAddress { get; init; } = EmailAddress.Empty;
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestOwner>(owner =>
            {
                owner.HasKey(x => x.Id);
                owner.HasEmailAddress(x => x.EmailAddress);
            });
    }
}