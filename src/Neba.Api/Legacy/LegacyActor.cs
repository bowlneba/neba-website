namespace Neba.Api.Legacy;

/// <summary>
/// Shared audit-attribution actor id for the Software's backdoor calls — stamped as a claim by
/// <see cref="LegacyApiKeyFilter"/> for the HTTP-level audit event, and set as the
/// <see cref="Neba.Api.Identity.AmbientActorContext"/> by each <c>*SyncJob</c> for its own
/// background-job audit events (Hangfire jobs have no HttpContext to read a claim from).
/// </summary>
internal static class LegacyActor
{
    public const string Id = "software-sync";
}