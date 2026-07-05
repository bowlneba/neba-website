using Audit.Core;
using Audit.WebApi;

using FastEndpoints;

using Microsoft.AspNetCore.Http;

using Neba.Api.Auditing;
using Neba.Api.Compliance;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class ApiAuditPayloadScrubbingActionTests
{
    private sealed class SampleRequest
    {
        public string Name { get; init; } = string.Empty;

        [PersonalData]
        public string Email { get; init; } = string.Empty;

        [PrivateData]
        public string Password { get; init; } = string.Empty;
    }

    private sealed class SampleResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    [Fact(DisplayName = "Scrub ignores non-WebApi audit events")]
    public void Scrub_ShouldNotThrow_WhenAuditEventIsNotWebApi()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);
        var auditEvent = new AuditEvent();

        // Act
        var exception = Record.Exception(() => sut.Scrub(auditEvent));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Scrub replaces the request body with a scrubbed projection when the endpoint's request DTO type is known")]
    public void Scrub_ShouldReplaceRequestBody_WithScrubbedProjection_WhenDtoTypeIsKnown()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EndpointDefinition(typeof(object), typeof(SampleRequest), typeof(SampleResponse))),
            "test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);

        var auditEvent = new AuditEventWebApi
        {
            Action = new AuditApiAction
            {
                RequestBody = new BodyContent { Value = """{"name":"Pat","email":"pat@example.com","password":"hunter2"}""" }
            }
        };

        // Act
        sut.Scrub(auditEvent);

        // Assert
        var scrubbed = auditEvent.Action.RequestBody!.Value.ShouldBeAssignableTo<IReadOnlyDictionary<string, object?>>()!;
        scrubbed.ShouldNotContainKey(nameof(SampleRequest.Password));
        scrubbed[nameof(SampleRequest.Email)].ShouldBe("p" + new string('*', "pat@example.com".Length - 1));
        scrubbed[nameof(SampleRequest.Name)].ShouldBe("Pat");
    }

    [Fact(DisplayName = "Scrub replaces the response body with a scrubbed projection when the endpoint's response DTO type is known")]
    public void Scrub_ShouldReplaceResponseBody_WithScrubbedProjection_WhenDtoTypeIsKnown()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EndpointDefinition(typeof(object), typeof(SampleRequest), typeof(SampleResponse))),
            "test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);

        var auditEvent = new AuditEventWebApi
        {
            Action = new AuditApiAction
            {
                ResponseBody = new BodyContent { Value = """{"id":"123"}""" }
            }
        };

        // Act
        sut.Scrub(auditEvent);

        // Assert
        var scrubbed = auditEvent.Action.ResponseBody!.Value.ShouldBeAssignableTo<IReadOnlyDictionary<string, object?>>()!;
        scrubbed[nameof(SampleResponse.Id)].ShouldBe("123");
    }

    [Fact(DisplayName = "Scrub clears the body value when no endpoint metadata is available")]
    public void Scrub_ShouldClearBodyValue_WhenEndpointMetadataIsMissing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);

        var auditEvent = new AuditEventWebApi
        {
            Action = new AuditApiAction
            {
                RequestBody = new BodyContent { Value = """{"name":"Pat"}""" }
            }
        };

        // Act
        sut.Scrub(auditEvent);

        // Assert
        auditEvent.Action.RequestBody!.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "Scrub clears the body value when there is no HttpContext")]
    public void Scrub_ShouldClearBodyValue_WhenHttpContextIsNull()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);

        var auditEvent = new AuditEventWebApi
        {
            Action = new AuditApiAction
            {
                RequestBody = new BodyContent { Value = """{"name":"Pat"}""" }
            }
        };

        // Act
        sut.Scrub(auditEvent);

        // Assert
        auditEvent.Action.RequestBody!.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "Scrub clears the body value when the captured content fails to deserialize into the DTO type")]
    public void Scrub_ShouldClearBodyValue_WhenDeserializationFails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EndpointDefinition(typeof(object), typeof(SampleRequest), typeof(SampleResponse))),
            "test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);

        var auditEvent = new AuditEventWebApi
        {
            Action = new AuditApiAction
            {
                RequestBody = new BodyContent { Value = "not valid json" }
            }
        };

        // Act
        var exception = Record.Exception(() => sut.Scrub(auditEvent));

        // Assert
        exception.ShouldBeNull();
        auditEvent.Action.RequestBody!.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "Scrub leaves a null body untouched")]
    public void Scrub_ShouldLeaveNullBodyUntouched()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new ApiAuditPayloadScrubbingAction(accessor);

        var auditEvent = new AuditEventWebApi
        {
            Action = new AuditApiAction { RequestBody = null }
        };

        // Act
        var exception = Record.Exception(() => sut.Scrub(auditEvent));

        // Assert
        exception.ShouldBeNull();
        auditEvent.Action.RequestBody.ShouldBeNull();
    }
}
