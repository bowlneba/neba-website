using System.Net.Http.Json;

using Audit.Core;
using Audit.Core.Providers;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neba.Api.Auditing;
using Neba.Api.Compliance;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[IntegrationTest]
[Component("Auditing")]
[Collection("AuditConfigurationSequential")]
public sealed class ApiAuditMiddlewareIntegrationTests : IAsyncLifetime
{
    private WebApplication _app = null!;

    public async ValueTask InitializeAsync()
    {
        Configuration.Setup()
            .Use(new InMemoryDataProvider())
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
        Configuration.ResetCustomActions();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<AuditEnrichmentAction>();
        builder.Services.AddSingleton<ApiAuditPayloadScrubbingAction>();
        builder.Services.AddFastEndpoints(o => o.Filter = type => type.DeclaringType == typeof(ApiAuditMiddlewareIntegrationTests));

        _app = builder.Build();

        var enrichmentAction = _app.Services.GetRequiredService<AuditEnrichmentAction>();
        Configuration.AddCustomAction(ActionType.OnEventSaving, enrichmentAction.OnEventSaving);

        var scrubbingAction = _app.Services.GetRequiredService<ApiAuditPayloadScrubbingAction>();
        Configuration.AddCustomAction(ActionType.OnEventSaving, scrubbingAction.OnEventSaving);

        _app.UseApiAuditMiddleware();
        _app.UseFastEndpoints();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
        Configuration.ResetCustomActions();
    }

    private static InMemoryDataProvider Provider => (InMemoryDataProvider)Configuration.DataProvider;

    [Fact(DisplayName = "A GET request does not produce an audit event")]
    public async Task Get_ShouldNotProduceAuditEvent()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        var response = await client.GetAsync(new Uri("/widgets/abc", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Provider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "A request to an excluded path does not produce an audit event")]
    public async Task Post_ShouldNotProduceAuditEvent_WhenPathIsExcluded()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        var response = await client.PostAsync(new Uri("/debug/ping", UriKind.Relative), content: null, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Provider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "A command endpoint produces an audit event with a scrubbed request and response body")]
    public async Task Post_ShouldProduceAuditEvent_WithScrubbedRequestAndResponseBody()
    {
        // Arrange
        using var client = _app.GetTestClient();
        using var payload = JsonContent.Create(new
        {
            name = "Pat",
            email = "pat@example.com",
            secret = "hunter2"
        });

        // Act
        var response = await client.PostAsync(new Uri("/widgets", UriKind.Relative), payload, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var auditEvent = Provider.GetAllEvents().ShouldHaveSingleItem().ShouldBeOfType<Audit.WebApi.AuditEventWebApi>();
        auditEvent.EventType.ShouldBe("Api:POST:/widgets");
        auditEvent.CustomFields["ActorId"].ShouldBe("anonymous");
        auditEvent.CustomFields.ShouldContainKey("CorrelationId");

        var requestBody = auditEvent.Action.RequestBody!.Value
            .ShouldBeAssignableTo<IReadOnlyDictionary<string, object?>>()!;

        requestBody.ShouldNotContainKey(nameof(CreateWidgetRequest.Secret));
        requestBody[nameof(CreateWidgetRequest.Email)].ShouldBe("p" + new string('*', "pat@example.com".Length - 1));
        requestBody[nameof(CreateWidgetRequest.Name)].ShouldBe("Pat");

        var responseBody = auditEvent.Action.ResponseBody!.Value
            .ShouldBeAssignableTo<IReadOnlyDictionary<string, object?>>()!;

        responseBody[nameof(CreateWidgetResponse.Id)].ShouldBe("widget-1");
    }

    public sealed class CreateWidgetRequest
    {
        public string Name { get; init; } = string.Empty;

        [PersonalData]
        public string Email { get; init; } = string.Empty;

        [PrivateData]
        public string Secret { get; init; } = string.Empty;
    }

    public sealed class CreateWidgetResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    public sealed class CreateWidgetEndpoint : Endpoint<CreateWidgetRequest, CreateWidgetResponse>
    {
        public override void Configure()
        {
            Post("/widgets");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CreateWidgetRequest req, CancellationToken ct)
            => await Send.OkAsync(new CreateWidgetResponse { Id = "widget-1" }, ct);
    }

    public sealed class GetWidgetEndpoint : EndpointWithoutRequest<CreateWidgetResponse>
    {
        public override void Configure()
        {
            Get("/widgets/{id}");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
            => await Send.OkAsync(new CreateWidgetResponse { Id = Route<string>("id") ?? string.Empty }, ct);
    }

    public sealed class DebugPingEndpoint : EndpointWithoutRequest
    {
        public override void Configure()
        {
            Post("/debug/ping");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
            => await Send.OkAsync(cancellation: ct);
    }
}