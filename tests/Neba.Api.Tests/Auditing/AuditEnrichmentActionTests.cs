using System.Security.Claims;

using Audit.Core;
using Audit.EntityFramework;

using Microsoft.AspNetCore.Http;

using Neba.Api.Auditing;
using Neba.Api.Compliance;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class AuditEnrichmentActionTests
{
    [Fact(DisplayName = "Enrich sets ActorId from the authenticated user's NameIdentifier claim")]
    public void Enrich_ShouldSetActorId_FromNameIdentifierClaim()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-123")], "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new AuditEnrichmentAction(accessor);
        var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object?>() };

        // Act
        sut.Enrich(auditEvent);

        // Assert
        auditEvent.CustomFields["ActorId"].ShouldBe("user-123");
    }

    [Fact(DisplayName = "Enrich sets ActorId to anonymous when there is no HttpContext")]
    public void Enrich_ShouldSetActorIdToAnonymous_WhenHttpContextIsNull()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new AuditEnrichmentAction(accessor);
        var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object?>() };

        // Act
        sut.Enrich(auditEvent);

        // Assert
        auditEvent.CustomFields["ActorId"].ShouldBe("anonymous");
    }

    [Fact(DisplayName = "Enrich sets CorrelationId from the HttpContext's TraceIdentifier when there is no active Activity")]
    public void Enrich_ShouldSetCorrelationId_FromTraceIdentifier()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new AuditEnrichmentAction(accessor);
        var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object?>() };

        // Act
        sut.Enrich(auditEvent);

        // Assert
        auditEvent.CustomFields["CorrelationId"].ShouldBe("trace-1");
    }

    [Fact(DisplayName = "Enrich sets CorrelationId to none when there is no HttpContext")]
    public void Enrich_ShouldSetCorrelationIdToNone_WhenHttpContextIsNull()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new AuditEnrichmentAction(accessor);
        var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object?>() };

        // Act
        sut.Enrich(auditEvent);

        // Assert
        auditEvent.CustomFields["CorrelationId"].ShouldBe("none");
    }

    [Fact(DisplayName = "Enrich does not attempt entity scrubbing for non-EF audit events")]
    public void Enrich_ShouldNotThrow_WhenAuditEventIsNotEntityFramework()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new AuditEnrichmentAction(accessor);
        var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object?>() };

        // Act
        var exception = Record.Exception(() => sut.Enrich(auditEvent));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Enrich scrubs EF entity snapshots using AuditPayloadScrubber classifications")]
    public void Enrich_ShouldScrubEntitySnapshots_ForEntityFrameworkEvents()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new AuditEnrichmentAction(accessor);

        var entry = new EventEntry
        {
            Entity = new SamplePayload { Name = "Pat", Email = "pat@example.com", Ssn = "123-45-6789" }
        };

        var auditEvent = new AuditEventEntityFramework
        {
            CustomFields = new Dictionary<string, object?>(),
            EntityFrameworkEvent = new EntityFrameworkEvent
            {
                Entries = [entry]
            }
        };

        // Act
        sut.Enrich(auditEvent);

        // Assert
        entry.ColumnValues.ShouldNotContainKey(nameof(SamplePayload.Ssn));
        entry.ColumnValues[nameof(SamplePayload.Email)].ShouldBe("p" + new string('*', "pat@example.com".Length - 1));
        entry.ColumnValues[nameof(SamplePayload.Name)].ShouldBe("Pat");
    }

    [Fact(DisplayName = "Enrich leaves ColumnValues untouched for EF entries with no attached entity")]
    public void Enrich_ShouldLeaveColumnValuesUntouched_WhenEntryHasNoEntity()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new AuditEnrichmentAction(accessor);

        var originalColumnValues = new Dictionary<string, object?> { ["Name"] = "Pat" };
        var entry = new EventEntry
        {
            Entity = null,
            ColumnValues = originalColumnValues!
        };

        var auditEvent = new AuditEventEntityFramework
        {
            CustomFields = new Dictionary<string, object?>(),
            EntityFrameworkEvent = new EntityFrameworkEvent
            {
                Entries = [entry]
            }
        };

        // Act
        sut.Enrich(auditEvent);

        // Assert
        entry.ColumnValues.ShouldBeSameAs(originalColumnValues);
    }

    private sealed class SamplePayload
    {
        public string Name { get; init; } = string.Empty;

        [PersonalData]
        public string Email { get; init; } = string.Empty;

        [PrivateData]
        public string Ssn { get; init; } = string.Empty;
    }
}