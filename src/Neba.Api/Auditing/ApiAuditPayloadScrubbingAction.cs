using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Audit.Core;
using Audit.WebApi;

using FastEndpoints;

using Neba.Api.Compliance;

namespace Neba.Api.Auditing;

/// <summary>
/// Replaces the raw request/response body Audit.WebApi's <see cref="AuditMiddleware"/> captures
/// with a scrubbed projection, keyed off the FastEndpoints request/response DTO types for the
/// endpoint that handled the request. Bodies that can't be mapped to a known DTO type (or fail to
/// deserialize) are dropped rather than stored unscrubbed — fail closed on PII.
/// </summary>
internal sealed class ApiAuditPayloadScrubbingAction(IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions DeserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void OnEventSaving(AuditScope scope) => Scrub(scope.Event);

    internal void Scrub(AuditEvent auditEvent)
    {
        if (auditEvent is not AuditEventWebApi apiEvent)
        {
            return;
        }

        var endpointDefinition = httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<EndpointDefinition>();

        ScrubBody(apiEvent.Action.RequestBody, endpointDefinition?.ReqDtoType);
        ScrubBody(apiEvent.Action.ResponseBody, endpointDefinition?.ResDtoType);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Audit scrubbing must never fail the request; an unscrubbable body is dropped, not stored raw.")]
    private static void ScrubBody(BodyContent? body, Type? dtoType)
    {
        if (body is null)
        {
            return;
        }

        if (dtoType is null || body.Value is null)
        {
            body.Value = null;
            return;
        }

        try
        {
            // Audit.WebApi captures JSON bodies either as a raw string or as an already-deserialized
            // object graph (e.g. Dictionary<string, object>) depending on content handling — round-trip
            // through JSON so both shapes bind onto the endpoint's actual request/response DTO type.
            var json = body.Value as string ?? JsonSerializer.Serialize(body.Value);
            var instance = JsonSerializer.Deserialize(json, dtoType, DeserializationOptions);
            body.Value = instance is null ? null : AuditPayloadScrubber.Scrub(instance);
        }
        catch (Exception)
        {
            body.Value = null;
        }
    }
}