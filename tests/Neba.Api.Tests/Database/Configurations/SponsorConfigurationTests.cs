using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contacts.Domain;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Sponsors")]
public sealed class SponsorConfigurationTests
{
    private readonly IEntityType _sponsorType;
    private readonly IEntityType _phoneNumberType;

    public SponsorConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _sponsorType = context.Model.FindEntityType(typeof(Sponsor))!;
        _phoneNumberType = _sponsorType
            .FindNavigation(nameof(Sponsor.PhoneNumbers))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to sponsors table in app schema")]
    public void Configure_ShouldMapToSponsorsTable()
    {
        // Act & Assert
        _sponsorType.GetTableName().ShouldBe("sponsors");
        _sponsorType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Id))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        // Act
        var alternateKey = _sponsorType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(Sponsor.Id)));

        // Assert
        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "name is varchar(63), not nullable")]
    public void Configure_ShouldConfigureNameColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Name))!;

        // Assert
        property.GetMaxLength().ShouldBe(63);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "slug is varchar(63), not nullable")]
    public void Configure_ShouldConfigureSlugColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Slug))!;

        // Assert
        property.GetMaxLength().ShouldBe(63);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "slug is an alternate key")]
    public void Configure_ShouldConfigureSlugAsAlternateKey()
    {
        // Act
        var alternateKey = _sponsorType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(Sponsor.Slug)));

        // Assert
        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "current_sponsor maps to current_sponsor column, not nullable")]
    public void Configure_ShouldConfigureIsCurrentSponsorColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.IsCurrentSponsor))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("current_sponsor");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "priority is not nullable")]
    public void Configure_ShouldConfigurePriorityColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Priority))!;

        // Assert
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "tier uses SmartEnumConverter, not nullable")]
    public void Configure_ShouldConfigureTierColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Tier))!;

        // Assert
        property.GetValueConverter().ShouldBeOfType<SmartEnumConverter<SponsorTier, int>>();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "category uses SmartEnumConverter, not nullable")]
    public void Configure_ShouldConfigureCategoryColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Category))!;

        // Assert
        property.GetValueConverter().ShouldBeOfType<SmartEnumConverter<SponsorCategory, int>>();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "website_url is varchar(255), nullable")]
    public void Configure_ShouldConfigureWebsiteUrlColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.WebsiteUrl))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("website_url");
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "tag_phrase is varchar(255), nullable")]
    public void Configure_ShouldConfigureTagPhraseColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.TagPhrase))!;

        // Assert
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "description is varchar(1023), nullable")]
    public void Configure_ShouldConfigureDescriptionColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.Description))!;

        // Assert
        property.GetMaxLength().ShouldBe(1023);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "live_read_text is varchar(2047), nullable")]
    public void Configure_ShouldConfigureLiveReadTextColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.LiveReadText))!;

        // Assert
        property.GetMaxLength().ShouldBe(2047);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "promotional_notes is varchar(4095), nullable")]
    public void Configure_ShouldConfigurePromotionalNotesColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.PromotionalNotes))!;

        // Assert
        property.GetMaxLength().ShouldBe(4095);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "facebook_url is varchar(255), nullable")]
    public void Configure_ShouldConfigureFacebookUrlColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.FacebookUrl))!;

        // Assert
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "instagram_url is varchar(255), nullable")]
    public void Configure_ShouldConfigureInstagramUrlColumn()
    {
        // Act
        var property = _sponsorType.FindProperty(nameof(Sponsor.InstagramUrl))!;

        // Assert
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "phone numbers map to sponsor_phone_numbers table in app schema")]
    public void Configure_ShouldMapPhoneNumbersToTable()
    {
        // Act & Assert
        _phoneNumberType.GetTableName().ShouldBe("sponsor_phone_numbers");
        _phoneNumberType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "phone numbers have sponsor_id foreign key")]
    public void Configure_ShouldConfigurePhoneNumberForeignKey()
    {
        // Act
        var fk = _phoneNumberType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == SponsorConfiguration.ForeignKeyName));

        // Assert
        fk.ShouldNotBeNull();
    }

    [Fact(DisplayName = "phone numbers have composite PK of sponsor_id and Type")]
    public void Configure_ShouldConfigurePhoneNumberCompositeKey()
    {
        // Act
        var pkPropertyNames = _phoneNumberType.FindPrimaryKey()!.Properties
            .Select(p => p.Name)
            .ToList();

        // Assert
        pkPropertyNames.ShouldContain(SponsorConfiguration.ForeignKeyName);
        pkPropertyNames.ShouldContain(nameof(PhoneNumber.Type));
    }
}
